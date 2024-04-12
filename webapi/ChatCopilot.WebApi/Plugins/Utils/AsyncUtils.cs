namespace ChatCopilot.WebApi.Plugins.Utils;

public class AsyncUtils
{
    public static async Task<T> SafeInvokeAsync<T>(Func<Task<T>> callback, string functionName)
    {
        try
        {
            return await callback();
        }
        catch (Exception ex)
        {
            throw new KernelException($"{functionName} failed.", ex);
        }
    }

    public static async Task SafeInvokeAsync(Func<Task> callback, string functionName)
    {
        try
        {
            await callback();
        }
        catch (Exception ex)
        {
            throw new KernelException($"{functionName} failed.", ex);
        }
    }
}
