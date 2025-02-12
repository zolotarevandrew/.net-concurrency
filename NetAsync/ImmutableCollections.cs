using System.Collections.Immutable;

namespace NetAsync;

public static class ImmutableCollections
{
    public static void Stack()
    {
        // 1 - создали пустой стэк singleton Empty
        // 2 - push, создали новый объект O(1), в head элемент, в tail предыдущий стэк
        // 3 - peek, o(1) просто отдает голову текущего стэка
        // 4 - pop, o(1) просто отдает tail текущего стэка
        // потоки не конкурируют, потому что не изменяют чужие данные, но жертвует аллокациями.
        
        ImmutableStack<int> stack = ImmutableStack<int>.Empty;
        stack = stack.Push(13);
        stack = stack.Push(7);
        
        foreach (int item in stack)
        {
            Console.WriteLine(item);
        }
        
        stack = stack.Pop();
        
        foreach (int item in stack)
        {
            Console.WriteLine(item);
        }
    }
    
    public static void Queue()
    {
        // 1 - создали пустую очередь singleton Empty, на двух стэках
        // Enqueue 13; 13 forwards -> backwards empty
        // Enqueue 7; 13 forwards -> backwards 7
        // Enqueue 15; 13 forwards -> backwards 15 7
        // Dequeue; 7 15 forwards -> backwards empty
        // Dequeue; 15 forwards -> backwards empty
        // Dequeue() выполняться за O(1) в среднем,
        
        ImmutableQueue<int> stack = ImmutableQueue<int>.Empty;
        stack = stack.Enqueue(13);
        stack = stack.Enqueue(7);
        
        
        
        foreach (int item in stack)
        {
            Console.WriteLine(item);
        }
        
        stack = stack.Dequeue();
        
        foreach (int item in stack)
        {
            Console.WriteLine(item);
        }
    }
    
    public static void List()
    {
        // работает через AVL дерево, все операции O(log n)
        
        ImmutableList<int> stack = ImmutableList<int>.Empty;
        stack = stack.Insert(0, 13);
        stack = stack.Insert(0, 7);
        
        
        
        foreach (int item in stack)
        {
            Console.WriteLine(item);
        }
    }
    
    public static void Set()
    {
        // работает через HAMT (Persistent Hash Trie), O(1) на вставку, удаление и поиск (почти).
        // Глубина дерева фиксирована (обычно 5-7 уровней), что делает операции почти O(1)
        // Когда добавляем элемент, сначала вычисляется его хеш
        // HAMT использует части хеш-кода для распределения по узлам:
        
        ImmutableHashSet<int> stack = ImmutableHashSet<int>.Empty;
        stack = stack.Add(13);
        stack = stack.Add(7);
        
        
        
        foreach (int item in stack)
        {
            Console.WriteLine(item);
        }
    }
    
    public static void Dictionary()
    {
        // работает через HAMT (Persistent Hash Trie), O(1) на вставку, удаление и поиск (почти).
        // Глубина дерева фиксирована (обычно 5-7 уровней), что делает операции почти O(1)
        // Когда добавляем элемент, сначала вычисляется его хеш
        // HAMT использует части хеш-кода для распределения по узлам:
        // HashBucket разрешают коллизии
        
        ImmutableDictionary<int, string> stack = ImmutableDictionary<int, string>.Empty;
        stack = stack.Add(1, "3");
        stack = stack.Add(2, "4");
        
        
        foreach (var item in stack)
        {
            Console.WriteLine(item.Key);
        }
    }
}