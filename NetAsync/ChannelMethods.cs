using System.Threading.Channels;

namespace NetAsync;

public static class ChannelMethods
{
    public static async Task Unbounded()
    {
        /*
         Внутри содержит ConcurrentQueue и создает Writer и Reader
         Writer 
         - на TryWrite крутится в цикле, и если _doneWriting - Exception не пустой то не дает записать
         - Далее проверяет пустой ли список BlockedReaders. они появляется когда в ReadAsync ожидает таску, а элементов в коллекции нету.
         Ждет асинхронно, чтобы продолжить когда следующий элемент окажется доступным. 
        BlockedReaders лежат в Deque - позволяет добавлять и удалять элементы с двух концов.
        Если blockedReaders нету, добавляет в ConcurrentQueue.
        _waitingReadersTail - хранит linkedList AsyncOperation.
        Операции добавляются в цепочку, когда вызывается WaitToReadAsync.
        
        В кейсах когда хотя бы 1 consumer асинхронно ждет ReadAsync, вместо добавления элемента в очередь - передадим это напрямую Consumer-у.
         
         
         Reader ReadAsync
         - если запрошена отмена выходим синхронно через ValueTask
         - пытаемся извлечь из concurrent queue синхронно и возвращаем ValueTask
         - еще раз пытаемся извлечь из concurrent queue синхронно , но уже в блокировке, другой поток мог записать данные до получения блокировки
         добавляем в blockedReaders

        */ 
        Channel<int> queue = Channel.CreateUnbounded<int>();
        ChannelWriter<int> writer = queue.Writer;
        await writer.WriteAsync(7);
        await writer.WriteAsync(13);
        writer.TryComplete(); 

        ChannelReader<int> reader = queue.Reader;
        await foreach (int value in reader.ReadAllAsync())
        {
            Console.WriteLine(value);
        }
    }
}