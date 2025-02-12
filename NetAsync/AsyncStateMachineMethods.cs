using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetAsync;


public class AsyncStateMachineMethods
{
    public Task Run()
    {
        /*
         1 - создали IAsyncStateMachine с дефолтным состоянием = -1 + taskawaiter + builder
         2 - реализовали void Method MoveNext + SetStateMachine
         3 - стартовали через Builder передая стейт машину по ссылке
         4 - вернули Task из билдера
         
         Start 
         - берем текущий Поток и сохраняем ExecutionContext +  SynchronizationContext
            ExecutionContext - управляет безопасной передачей информации между потоками (например, идентификаторы пользователя, культура и безопасность).
            SynchronizationContext - управляет привязкой выполнения к конкретному контексту, например, UI-потоку в WinForms или WPF.
        - вызываем MoveNext
        - возвращаем SynchronizationContext если они изменился
        - возвращаем ExecutionContext если он изменился и нотифицируем OnValueChange для IAsyncLocal
          (Значение AsyncLocal<T> сохраняется в ExecutionContext, который переносится между await.)
          AsyncLocal напрямую берет данные и устанавливает в ExecutionContext
          
        TaskAwaiter - структура, внутри лежит ссылка на Task на OnCompleted шедулит Continuation далее
        - task.SetContinuationForAwait(continuation, continueOnCapturedContext, flowExecutionContext);
          
        MoveNext
        - берем Awaiter
        - проверяем не завершен ли он уже
        - вызываем AwaitUnsafeOnCompleted
        - соот-во у AsyncTaskMethodBuilder cвойство Task преобразуется в AsyncStateMachineBox<TStateMachine> и хранит там стейт машину
        - у TaskAwaiter-а есть таска и туда складывается continuation task.UnsafeSetContinuationForAwait(stateMachineBox, continueOnCapturedContext);
        TaskAwaiter.Task -> AsyncStateMachineBox
        - cоздается TaskSchedulerAwaitTaskContinuation, который вызывает MoveNext у AsyncStateMachineBox как continuation
        
        AwaitUnsafeOnCompleted
        - боксит нашу iasyncstatemachine в реализацию КЛАСС AsyncStateMachineBox<TStateMachine> box
         которая наследует Task<TResult>, IAsyncStateMachineBox
         - вызывает у taskawaiter-а TaskAwaiter.UnsafeOnCompletedInternal(ta.m_task, box, continueOnCapturedContext: true);
         - если таска не завершена, то добавляет continuation напрямую в очередь ThreadPool
             ThreadPool.UnsafeQueueUserWorkItemInternal(stateMachineBox, preferLocal: true);
         - 
        
        
         
        */
        
        RunStruct stateMachine = default;
        stateMachine._builder = AsyncTaskMethodBuilder.Create();
        stateMachine._state = -1;
        stateMachine._client = new HttpClient();
        stateMachine._builder.Start(ref stateMachine);
        return stateMachine._builder.Task;
    }

    [StructLayout(LayoutKind.Auto)]
    private struct RunStruct : IAsyncStateMachine
    {
        public int _state;
        public AsyncTaskMethodBuilder _builder;
        private TaskAwaiter<HttpResponseMessage> _awaiter;
        public HttpClient _client;

        void IAsyncStateMachine.MoveNext()
        {
            int num1 = _state;
            var thread = Thread.CurrentThread.ManagedThreadId;
            try
            {
                TaskAwaiter<HttpResponseMessage> awaiter;
                int num2;
                if (num1 != 0)
                {
                    awaiter = _client.GetAsync("https://google.com").GetAwaiter();
                    if (!awaiter.IsCompleted)
                    {
                        _state = num2 = 0;
                        _awaiter = awaiter;
                        _builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
                        return;
                    }
                }
                else
                {
                    awaiter = _awaiter;
                    _awaiter = new TaskAwaiter<HttpResponseMessage>();
                    _state = num2 = -1;
                }

                awaiter.GetResult();
                for (int i = 0; i < 3; ++i)
                    Console.WriteLine(i);
            }
            catch (Exception ex)
            {
                _state = -2;
                _builder.SetException(ex);
                return;
            }

            _state = -2;
            _builder.SetResult();
        }

        void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _builder.SetStateMachine(stateMachine);
        }
    }
}