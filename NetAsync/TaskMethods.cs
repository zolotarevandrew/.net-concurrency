using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace NetAsync;

public static class TaskMethods
{
    public static void Start()
    {
        var task = new Task(() => {Console.WriteLine("test"); });
        task.Start();
        
        /*
         * https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/ThreadPoolTaskScheduler.cs
         * protected internal override void QueueTask(Task task)
        {
            TaskCreationOptions options = task.Options;
            if (Thread.IsThreadStartSupported && (options & TaskCreationOptions.LongRunning) != 0)
            {
                // Run LongRunning tasks on their own dedicated thread.
                new Thread(s_longRunningThreadWork)
                {
                    IsBackground = true,
                    Name = ".NET Long Running Task"
                }.UnsafeStart(task);
            }
            else
            {
                // Normal handling for non-LongRunning tasks.
                ThreadPool.UnsafeQueueUserWorkItemInternal(task, (options & TaskCreationOptions.PreferFairness) == 0);
            }
        }
         */
        Task.Factory.StartNew(() =>
        {
            Console.WriteLine("Runs in dedicated thread");
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        
        
        /*
         * task.Wait - блокирует поток через ManualResetEventSlim
         * 
         */
        task.Wait();
    }
    
    public static Task All(this IEnumerable<Task> waiters)
    {
        //дополнительные аллокации внутри
        return Task.WhenAll(waiters);
    }

    public static Task All(Task task1, Task task2)
    {
        //также дополнительные аллокации
        //превратится в return Task.WhenAll(new Task[2]{ task1, task2 });

        // 0 - новый класс WhenAllPromise наследует Task и ITaskCompletionAction
        // 1 - для каждой таски добавляет CompleteAction
        // 2 - хранит _remainingToComplete, сколько осталось подождать еще задач
        // 3 - Если всего один неуспешный Task → нет лишнего List<Task>
        // 4 - List<Task> создается только при втором неуспешном Task
        // 5 - далее идем в cas и пытаемся привести к list-у 
        // в конце декрементит _remainingToComplete и если 0 и нет ошибок, то завершает задачу.


        return Task.WhenAll(task1, task2);
    }

    public static Task Any(this IEnumerable<Task> waiters)
    {
        //дополнительные аллокации внутри
        return Task.WhenAny(waiters);
    }

    public static Task Any(Task task1, Task task2)
    {
        //также дополнительные аллокации
        //превратится в return Task.WhenAll(new Task[2]{ task1, task2 });

        // 0 - новый класс TwoTaskWhenAnyPromise наследует Task и ITaskCompletionAction
        // 1 - для каждой таски добавляет CompleteAction в конструкторе
        // 2 - Interlocked.Exchange(ref _task1, null)) кто первый успел тот и попадет в Invoke условие
        // 3 - убираем continuation и подчищаем ссылку на _task2, для gc
        // 4 - вызываем RemoveContinuation 

        //нет дополнительных аллокаций для двух элементов, передаются напрямую.
        return Task.WhenAny(task1, task2);
    }

    public static Task<TResult> Result<TResult>(TResult result)
    {
        //для простых типов таски кэшируется, иначе будет создана новая таска
        //кэшировать внутри лучше для неизменяемых объектов
        //для изменяемых объектов лучше кэшировать сам объект, а Task.FromResult создавать каждый раз заново.
        return Task.FromResult<TResult>(result);
    }

    public static Task<Task<T>> [] Interleaved<T>(IEnumerable<Task<T>> tasks)
    {
        //1 - O(N), а не O(N^2) через WhenAny
        //2 - аллоцируем список, чтобы не было проблем с enumeration
        //3 - создаем TaskCompletionSource и Tasks как наши результаты, ссылающиеся на TaskCompletionSource
        //4 - добавляем Continuation всем выполняемым задачам, с помощью TaskCompletionSource и потокобезопасно берем индекс через Interlocked
        //5 - профит
        
        var inputTasks = tasks.ToList();

        var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
        var results = new Task<Task<T>>[buckets.Length];
        for (int i = 0; i < buckets.Length; i++) 
        {
            buckets[i] = new TaskCompletionSource<Task<T>>();
            results[i] = buckets[i].Task;
        }

        int nextTaskIndex = -1;
        Action<Task<T>> continuation = completed =>
        {
            var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
            bucket.TrySetResult(completed);
        };

        foreach (var inputTask in inputTasks)
            inputTask.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return results;
    }

}