namespace NetAsync;

public static class ThreadMethods
{
    public static void StartAndJoin()
    {
        var thread = new Thread(() =>
        {
            Console.WriteLine("test");
        });
        thread.Start();

        /*
         * thread.Join() нельзя вызывать в том же потоке, иначе будет deadlock
         * thread.Start нельзя вызывать повторно
         * thread.UnsafeStart не захватывает executioncontext и не может работать с async local
         * thread.GetApartmentState - MTA многопоточная среда, STA однопоточная, нельзя использовать в async/await, потоки тредпула все MTA
         * thread.Interrupt() не завершает поток мгновенно. Он вызывает ThreadInterruptedException только если поток находится в WaitSleepJoin.
            Если поток работает без ожидания, Interrupt() ничего не делает.
         * Thread.Sleep(0) передает управление любому потоку (на любом ядре).
         * Thread.Sleep - Поток перемещается в список ожидающих. Поток не получает процессорное время до завершения задержки
         * Thread.Yield() передает управление только потокам на том же ядре
         * Thread.SpinWait - активное ожидание" (busy-wait) → загружает процессор. Полезен для коротких ожиданий
            Каждая итерация выполняет несколько инструкций процессора. Задержка НЕ равна iterations в процессорных тактах.
            
         */ 
        thread.Join();
        

    }
}