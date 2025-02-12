namespace NetAsync;

public static class MyDelay
{
    //DelayPromise ITimer? _timer
    public static Task Run(int milliseconds)
    {
        return Task.Delay(TimeSpan.FromMilliseconds(milliseconds));
    }

    public static Task CustomRun(int milliseconds)
    {
        var timerTask = new TimerTask(milliseconds);
        return timerTask.Task;
    }
    
    public class TimerTask
    {
        private readonly Timer _timer;
        private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TimerTask(int millisecondsDelay)
        {
            _timer = new Timer(_ =>
            {
                _tcs.TrySetResult(true);
                DisposeTimer();
            }, null, millisecondsDelay, Timeout.Infinite);
        }

        public Task Task => _tcs.Task;

        private void DisposeTimer()
        {
            try
            {
                _timer.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Игнорируем, если таймер уже был удалён
            }
        }
        
    }
    
    //inherits from Task
    //TBD TimerQueueTimer, TimeProvider.CreateTime
    /*
    public class SourceDelayPromise 
        {
            private static readonly TimerCallback s_timerCallback = TimerCallback;
            private readonly ITimer? _timer;

            public SourceDelayPromise(uint millisecondsDelay, TimeProvider timeProvider)
            {
             
                using (ExecutionContext.SuppressFlow())
                {
                    _timer = timeProvider.CreateTimer(s_timerCallback, this, TimeSpan.FromMilliseconds(millisecondsDelay), Timeout.InfiniteTimeSpan);
                }

                if (IsCompleted)
                {
                    // Handle rare race condition where the timer fires prior to our having stored it into the field, in which case
                    // the timer won't have been cleaned up appropriately.  This call to close might race with the Cleanup call to Close,
                    // but Close is thread-safe and will be a nop if it's already been closed.
                    _timer.Dispose();
                }
            }

            public bool IsCompleted { get; set; }

            private static void TimerCallback(object? state) => ((SourceDelayPromise)state!).CompleteTimedOut();

            private void CompleteTimedOut()
            {
                if (TrySetResult())
                {
                    Cleanup();
                }
            }

            //Completes a promise task as RanToCompletion.
            public bool TrySetResult()
            {
                return false;
            }

            protected virtual void Cleanup() => _timer?.Dispose();
        }
        */
}