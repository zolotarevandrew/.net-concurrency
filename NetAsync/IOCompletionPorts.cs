using System.Net.Sockets;

namespace NetAsync;

public static class IOCompletionPorts
{
    /*
     *    IOCP - https://medium.com/%40rahulsingh_88457/visualizing-i-o-completion-ports-based-async-i-o-on-windows-8773d51ac19f
         1) Allocate special structure in the user space heap + data buffer which will be moved later
         2) Create device object in kernel space 
         3) Register handle by using -  CreateIoCompletionPort  in kernel space
         4) Issue I/O operation. Since the device handle was created with async flag set, 
         the API returns without waiting for the underlying I/O operation to finish.
         5) At some point in future, the I/O operation finishes - Device driver pushes  PostQueuedCompletionStatus to IOCP
         - When IOCP gets notified, it wakes up one of the threads blocking on it via the GetQueuedCompletionStatus function
         - 
         https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Threading/PortableThreadPool.IO.Windows.cs
         
         
     */

    public static void Threads()
    {
 
        /*
         * Поллер в цикле крутит и GetQueuedCompletionStatusEx ждет ответа от ОС пока его разбудят, чтобы положить IO в очередь на обработку
         * кладет в https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Threading/ThreadPoolWorkQueue.cs#L1287
         * https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Sockets/src/System/Net/Sockets/SafeSocketHandle.Windows.cs#L30
         * internal unsafe SocketError DoOperationSendSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncCompletionOwnership == 0, $"Expected 0, got {_asyncCompletionOwnership}");

            fixed (byte* bufferPtr = &MemoryMarshal.GetReference(_buffer.Span))
            {
                NativeOverlapped* overlapped = AllocateNativeOverlapped();
                try
                {
                    var wsaBuffer = new WSABuffer { Length = _count, Pointer = (IntPtr)(bufferPtr + _offset) };

                    SocketError socketError = Interop.Winsock.WSASend(
                        handle,
                        &wsaBuffer,
                        1,
                        out int bytesTransferred,
                        _socketFlags,
                        overlapped,
                        IntPtr.Zero);

                    return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, ref overlapped, _buffer, cancellationToken);
                }
                catch when (overlapped is not null)
                {
                    FreeNativeOverlapped(ref overlapped);
                    throw;
                }
            }
        }
         */
        /*
         * _nativeEvents =
                        (Interop.Kernel32.OVERLAPPED_ENTRY*)
                        NativeMemory.Alloc(NativeEventCapacity, (nuint)sizeof(Interop.Kernel32.OVERLAPPED_ENTRY));
                    _events = new ThreadPoolTypedWorkItemQueue();

                    // These threads don't run user code, use a smaller stack size
                    _thread = new Thread(Poll, SmallStackSizeBytes);

                    // Poller threads are typically expected to be few in number and have to compete for time slices with all
                    // other threads that are scheduled to run. They do only a small amount of work and don't run any user code.
                    // In situations where frequently, a large number of threads are scheduled to run, a scheduled poller thread
                    // may be delayed artificially quite a bit. The poller threads are given higher priority than normal to
                    // mitigate that issue. It's unlikely that these threads would starve a system because in such a situation
                    // IO completions would stop occurring. Since the number of IO pollers is configurable, avoid having too
                    // many poller threads at higher priority.
                    if (IOCompletionPollerCount * 4 < Environment.ProcessorCount)
                    {
                        _thread.Priority = ThreadPriority.AboveNormal;
                    }
         */
    }
}