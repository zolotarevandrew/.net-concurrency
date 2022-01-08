using System.Runtime.CompilerServices;

namespace AsyncPromise
{
    [AsyncMethodBuilder(typeof(AsyncPromiseBuilder<>))]
    public class AsyncPromise<T> : INotifyCompletion
    {
        Action _continuation;
        public T Result { get; private set; }
        public bool IsCompleted { get; private set; }
        public AsyncPromise<T> GetAwaiter() => this;

        public void SetResult(T value)
        {
            Result = value;
            IsCompleted = true;
            _continuation?.Invoke();
        }

        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
            if (IsCompleted) _continuation();
        }

        public T GetResult() => Result;
    }
}
