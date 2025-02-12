using System.Collections.Concurrent;

namespace NetAsync;

public static class ThreadSafeCollections
{
    public static void ConcurrentDictionary()
    {
        /*
          AddOrUpdate и GetOrAdd принимают делегаты. 
          они могут быть вызваны несколько раз в конкурентной среде, 
          Это может привести к созданию нескольких экземпляров значения, хотя в словаре будет сохранен только один из них
          
          https://habr.com/ru/companies/skbkontur/articles/348508/
          
          1) DefaultCapacity - 31, кол-во бакетов
          2) DefaultConcurrencyLevel - кол-во процессоров
          
          Имеется небольшой массив обычных локов, и каждый из них отвечает за целый диапазон bucket-ов (отсюда и stripe в названии). 
          Для того, чтобы записать что-то в произвольный bucket, необходимо захватить соответствующий ему лок.
          
          3) Tables.buckets - основной массив "бакетов", где хранятся элементы.
            Каждый бакет (VolatileNode) представляет односвязный список для разрешения коллизий.
            
            _fastModBucketsMultiplier - оптимизация для быстрого вычисления, чтобы не делить а сдвигать
            _locks - массив блокировок для отдельных секций таблицы - блокируются только части таблицы, а не весь словарь
            _countPerLock - Счетчик количества элементов в каждом "сегменте", который защищен locks[i]
            
            Свойства Count и IsEmpty коварно захватывают все локи в словаре. 
            Лучше воздержаться от частого вызова этих свойств из нескольких потоков
            
            Свойства Keys и Values еще более коварны: они не только берут все локи, но и целиком копируют в отдельный List все ключи и значения
            
            В отличие от обычного Dictionary, можно производить вставку в ConcurrentDictionary или удаление из него прямо во время перечисления!
        
         */

        var lazy = new Lazy<int>();
        
        var dictionary = new ConcurrentDictionary<int, string>();
        string newValue = dictionary.AddOrUpdate(0, 
            key => "Zero", 
            (key, oldValue) => "Zero");
    }
    
    public static void BlockingCollection()
    {
        /*
          Базовая коллекция: BlockingCollection<T> использует внутреннюю коллекцию, реализующую интерфейс IProducerConsumerCollection<T>. 
          По умолчанию это ConcurrentQueue<T>, но можно использовать и другие коллекции, такие как ConcurrentStack<T> или ConcurrentBag<T>.
          
          Блокировка и ожидание: Методы Add() и Take() могут блокировать выполнение потока, если коллекция переполнена или пуста соответственно. Это позволяет синхронизировать работу между производителями и потребителями.

          Ограничение размера: Можно задать максимальный размер коллекции. В этом случае метод Add() будет блокировать поток, если коллекция достигла предела, до тех пор, пока не освободится место.
        
          Таким образом, BlockingCollection<T> предоставляет удобный и эффективный механизм для организации потокобезопасного обмена данными между производителями и потребителями
         */
        
        
        var collection = new BlockingCollection<int>();
        collection.Add(1);
        collection.Add(2);
        collection.CompleteAdding();
        
        
    }
}