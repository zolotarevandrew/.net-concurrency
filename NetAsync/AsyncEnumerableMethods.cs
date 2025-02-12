namespace NetAsync;

public static class AsyncEnumerableMethods
{
    public static async IAsyncEnumerable<int> Run(IAsyncEnumerable<int> enumerable)
    {
        await foreach (var item in enumerable)
        {
            yield return item;
        }
    }
}