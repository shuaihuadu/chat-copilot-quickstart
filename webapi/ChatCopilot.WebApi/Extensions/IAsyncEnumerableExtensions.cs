namespace ChatCopilot.WebApi.Extensions;

public static class IAsyncEnumerableExtensions
{
    internal static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        List<T> result = [];

        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }
}