namespace NetAsync;

public class Program
{
    private static readonly Random _random = new();
    
    public static ValueTask<int> GetCachedOrComputeAsync()
    {
        if (_random.Next(0, 2) == 0)
        {
            // Возвращаем значение сразу, без создания Task
            return new ValueTask<int>(42);
        }
        
        // Асинхронная операция, создание Task неизбежно
        return new ValueTask<int>(ComputeAsync());
    }

    private static async Task<int> ComputeAsync()
    {
        await Task.Delay(100);
        return 42;
    }

    public static async Task Main()
    {
        int result = await GetCachedOrComputeAsync();
        Console.WriteLine($"Result: {result}");
    }
}