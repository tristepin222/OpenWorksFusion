
public static class ContentErrorNotifier
{
    // Define the delegate and event
    public delegate void ContentErrorHandler(string errorMessage);
    public static event ContentErrorHandler OnContentError;

    // Static method to invoke the error callback
    public static void NotifyError(string errorMessage)
    {
        OnContentError?.Invoke(errorMessage);
    }
}
