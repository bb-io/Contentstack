using Contentstack.Management.Core.Exceptions;

namespace Apps.Contentstack.Utils;

public static class ContentstackErrorHandler
{
    public static async Task<T> HandleRequest<T>(Func<Task<T>> func)
    {
        try
        {
            return await func();
        }
        catch (ContentstackErrorException ex)
        {
            throw new(ex.ErrorMessage);
        }
    }
}