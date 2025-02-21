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


    public static void ManualResetEvent()
    {
        // 1) реализует idisposable
        //создает ядерный объект "событие" через CreateEvent
        ManualResetEvent @event = new(false);
        
        //вызывает NtWaitForSingleObject(), ожидая, пока событие будет установлено Set()
        @event.WaitOne();

        //вызывает SetEvent(), переводя объект в сигнальное состояние
        //Все потоки выходят из WaitOne(), потому что ManualResetEvent не сбрасывается автоматически
        @event.Set();
        
        //сбрасывает объект, заставляя WaitOne() снова блокироваться
        @event.Reset();
    }
    
    public static void AutoResetEvent()
    {
        // 1) реализует idisposable
        //создает ядерный объект "событие" через CreateEvent
        AutoResetEvent @event = new(false);
        
        //вызывает NtWaitForSingleObject(), ожидая, пока событие будет установлено Set()
        @event.WaitOne();

        //вызывает SetEvent(), переводя объект в сигнальное состояние
        //разблокирует один поток и сбрасывается автоматически
        @event.Set();
        
        //Reset не нужен, сбрасывается сам
        //@event.Reset();
    }
    
    public static void ManualResetEventSlim()
    {
        // Использовать для разблокирования всех потоков, которые ждали в определенном месте.
        // 1) реализует idisposable
        ManualResetEventSlim @event = new(false);
        
        /*
         сначала крутится в SpinWait цикле и ждет
         поток становится в очередь ожидания Monitor.Wait внутри Lock-a и еще в цикле
        */
        @event.Wait();

        //вызывает Monitor.PulseAll если более 1 ожидающего потока
        @event.Set();
        
        //ставит isset в false
        @event.Reset();
    }
    
    public static void CountDown()
    {
        // Использовать для ожидания завершения N потоков, например
        // 1) реализует idisposable
        // ManualResetEventSlim внутри
        CountdownEvent @event = new(5);
        
        /*
         вызываем ManualResetEventSlim Wait
        */
        @event.Wait();

        // через Interlocked уменьшаем счетчик, и если 0 то вызываем Set у ManualResetSlim-а, чтобы Wait освободилось
        @event.Signal();
        
    }
    
    public static void SpinLockMethods()
    {
        // Использовать для коротких ожиданий без переключения контекста потока 10-100 тактов.
        SpinLock _spinLock = new();
        
        bool lockTaken = false;
            /*
             1) пытается захватить через CompareExсhange
             2) если блокировка свободна еще раз пытается захватить через CompareExсhange
             3) если уже есть ожидающие потоки увеличиваем счетчик
             4) в цикле пытаемся сделать CAS
             */
        _spinLock.Enter(ref lockTaken);
        try
        {
            // Короткий критический код
            Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] Обновление данных...");
        }
        finally
        {
            //interlocked decrement или exchange
            if (lockTaken) _spinLock.Exit();
        }
        
    }
    
    public static void ReaderWriterLock()
    {
        // Реализует idisposable
        using ReaderWriterLockSlim lck = new();
        
        /*
         * при создании устанавливается уникальный _lockID - есть статический s_nextLockID (сначала он 0) и при создании инстанса он увеличивает на 1 каждый раз.
         * то есть у каждого ReaderWriterLockSlim уникальный _lockId.
         * При EnterReadLock
         * 
         * 1) если id текущего потока равен id читателя кидаем exception
         * 2) Пытаемся зайти в spinwait lock
         * через Interlocked.CompareExchange(ref _isLocked, 1, 0) == 0
         * внутри не использует Sleep(1), а только Sleep(0), и SpinWait,  потому что может привести к latency в reader/writer lock операциях
         *
         * Получаем ReaderWriterCount - связанный список в котором у каждого потока указана нужное количество необходимое для получения лока по _lockId.
         * ReaderWriterCount ThreadStatic - уникален для каждого потока, чтобы не создавать блокировки.
         * Крутимся в цикле
         * - максимальное количество ридеров MAX_READERS - 268435454 (0x10000000 - 2), если _owner < MAX_READERS, то можем войти в блокировку, увеличиваем кол-во овнеров
         * и кол-во readercount у ReaderWriterCount
         * - если таймаут исчерпался выходим
         * - проверяем имеет ли смысл крутиться в цикле дальше и при необходимости меняем ReaderWriterCount, поскольку могла произойти смена потоков
         * - создаем EventWaitHandle и ждем Set пока не освободят
         *
         * При ExitReadLock в простом случае
         * - заходим в spinlock
         * - освобождаем spinlock
         */
        lck.EnterReadLock();
        try
        {
            Console.WriteLine($"Reader");
        }
        finally
        {
            lck.ExitReadLock();
        }
        Thread.Sleep(1000);
        
    }

    public void SpinWait()
    {
        SpinWait spinWait = new SpinWait();
        /*
         * Сначала ждет активно (SpinWait).
        Если ожидание долгое, уступает управление (Thread.Yield()).
        Если ожидание слишком долгое, засыпает (Thread.Sleep(0) / Thread.Sleep(1)).
         */
        spinWait.SpinOnce();
    }
    
    public void Lazy()
    {
        Lazy<string> lazyValue = new Lazy<string>(() =>
        {
            Console.WriteLine("Объект создается...");
            return "Hello, Lazy!";
        });
        var res = lazyValue.Value;

    }
}