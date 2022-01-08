using Queue = System.Collections.Concurrent.ConcurrentQueue<System.Threading.SendOrPostCallback>;

namespace Schedulers
{
    public class CustomSyncContext : SynchronizationContext, IDisposable
    {
        const int THREAD_COUNT = 8;
        Queue _workItems;
        ManualResetEvent _mre;
        volatile bool _isDisposed;
        volatile int _notStartedThreadsCount;

        public CustomSyncContext()
        {
            _mre = new ManualResetEvent(false);
            _workItems = new Queue();
            _isDisposed = false;
            _notStartedThreadsCount = THREAD_COUNT;

            for (int i = 1; i <= THREAD_COUNT; i++)
            {
                var newI = i;
                Thread thread = new Thread(() => TryDoWork(newI));
                thread.Start();
            }
            CheckAllThreadsStarted();
        }

        void CheckAllThreadsStarted()
        {
            do { }
            while (Interlocked.CompareExchange(ref _notStartedThreadsCount, 0, 0) != 0);
            Console.WriteLine($"All threads have started {_notStartedThreadsCount == 0}");
        }

        void CheckAllThreadsFinished()
        {
            do {
                Console.WriteLine(_notStartedThreadsCount);
            }
            while (Interlocked.CompareExchange(ref _notStartedThreadsCount, -1, Environment.ProcessorCount) != Environment.ProcessorCount);
            Console.WriteLine($"All threads have finished {_notStartedThreadsCount == -1}");
        }

        void TryDoWork(int index)
        {
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} started do work {index}");
            Interlocked.Decrement(ref _notStartedThreadsCount);
            var spinWait = new SpinWait();
            while (!_isDisposed)
            {
                bool isDeququed = _workItems.TryDequeue(out var callback);
                if (isDeququed)
                {
                    callback(null);
                    continue;
                }

                _mre.Reset();

                if (spinWait.NextSpinWillYield)
                {
                    Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} WAIT ONE");
                    _mre.WaitOne();
                    spinWait.Reset();
                }
                spinWait.SpinOnce();
            }

            Interlocked.Increment(ref _notStartedThreadsCount);
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} finished do work {index}");
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _workItems.Enqueue(d);
            _mre.Set();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _mre.Set();

            CheckAllThreadsFinished();
            _mre.Close();

            _mre = null;
            _workItems = null;
        }
    }
}
