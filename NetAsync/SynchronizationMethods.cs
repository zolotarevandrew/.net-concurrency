using System.Runtime.CompilerServices;

namespace NetAsync;

public class SynchronizationMethods
{
    private static object _lock = new object();
    public void Lock()
    {
        object lck = _lock;
        bool lockTaken = false;
        try
        {
            /*
             * 1) Поддерживает рекурсивные блокировки, один и тот же поток может многократно входить в один и тот же блок lock без возникновения взаимоблокировки.
             * Счетчик входов увеличивается при каждом входе и уменьшается при выходе, и только после достижения нуля блокировка освобождается
             *
             * 2) В большинстве случаев операции блокировки и разблокировки выполняются в пользовательском режиме, что обеспечивает высокую производительность.
             * Однако, если потокам приходится долго ожидать освобождения блокировки, система может переключить их в режим ядра для более эффективного управления ожиданием.
             * Это связано с использованием объектов синхронизации ядра, таких как события (Event objects), которые создаются лениво, только при необходимости.
             *
             * 3) 
             */
            Monitor.Enter(lck, ref lockTaken);
        }
        finally
        {
            if (lockTaken)
                Monitor.Exit(lck);
        }
    }

    public static async Task SemaphoreSlim()
    {
        /*
         * 1) Реализует IDisposable для async версии очищает ссылки на head, tail таски
         * - В WaitAsync
         *  ставит lock, если кол-во counter-ов доступно, декрементирует и возвращает Task.FromResult(true)
         *  интересно, что сделали на bool, потому что Task<bool> кэшируется платформой.
         *  Если нет достуных слотов и таймаут 0, то сразу возвращает Task.FromResult(false)
         *  Если слоты недоступны, хранит linkedList Task у себя отдельно
         *  Если был указан таймаут, ждет асинхронно через СancellationPromise
         *    await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
         * 2) Release
         *  попадаем в lock, если свободных слотов нет, то кидаем exception SemaphoreFull
         *  освобождаем тех кто попал в lock через MonitorPulse, освобождаем очередь ожидающих в linkedlist
         */
        SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
        int value = 5;

        async Task Try()
        {
            //будет ждать завершенной таски, если успешно зашел в семафор, значит либо зашло в первый раз через Task.FromResult
            //либо был освобожден Release, где ожидающей таске проставился setresult(true) и выполнение возможно дальше.
            await mutex.WaitAsync(); 
            
            try 
            { 
                int oldValue = value; 
                await Task.Delay(TimeSpan.FromSeconds(oldValue)); 
                value = oldValue + 1; 
            }
            finally 
            { 
                mutex.Release(); 
            }
        }

        var task1 = Task.Run(Try);
        var task2 = Task.Run(Try);

        await Task.WhenAll(task1, task2);
        
        Console.WriteLine(value);


    }
}