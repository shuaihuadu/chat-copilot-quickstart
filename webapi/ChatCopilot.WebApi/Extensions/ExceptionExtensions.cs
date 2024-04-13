namespace ChatCopilot.WebApi.Extensions;

internal static class ExceptionExtensions
{
    internal static bool IsCriticalException(this Exception ex)
        => ex is OutOfMemoryException
        or ThreadAbortException
        or AccessViolationException
        or AppDomainUnloadedException
        or BadImageFormatException
        or CannotUnloadAppDomainException
        or InvalidProgramException
        or StackOverflowException;
}
