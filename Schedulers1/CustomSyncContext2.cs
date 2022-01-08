using System;
using System.Threading;
using Queue = System.Collections.Concurrent.ConcurrentQueue<System.Threading.SendOrPostCallback>;

namespace Concurrent.Schedulers
{
    public class CustomSyncContext2 : SynchronizationContext, IDisposable
    {
        const int THREAD_COUNT = 8;
        Queue _workItems;
        ManualResetEvent _mre;
        volatile bool _isDisposed;
        CountdownEvent _countEvt;
        Barrier _barrier;

        public CustomSyncContext2()
        {
            _mre = new ManualResetEvent(false);
            _workItems = new Queue();
            _isDisposed = false;
            _countEvt = new CountdownEvent(THREAD_COUNT);
            _barrier = new Barrier(THREAD_COUNT + 1);

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
            _countEvt.Wait();
            Console.WriteLine($"All threads have started");
        }

        void CheckAllThreadsFinished()
        {
            Console.WriteLine($"All threads have finished");
        }

        void TryDoWork(int index)
        {
            _countEvt.Signal();
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} started do work {index}");
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
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} finished do work {index}");
            _barrier.SignalAndWait();
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
            _mre.Close();
            _countEvt.Dispose();

            _barrier.SignalAndWait();
            CheckAllThreadsFinished();
            _barrier.Dispose();

            _mre = null;
            _workItems = null;
        }
    }
}
