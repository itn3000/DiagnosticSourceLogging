namespace DiagnosticSourceLogging
{
    public static class Constants
    {
        public const string EventObserverCompletedEventName = EventObserverCompletedArg.Name;
        public const string EventObserverErrorEventName = EventObserverErrorArg.Name;
        public const string EventObserverProcessErrorEventName = EventObserverProcessErrorArg.Name;
        public const string EventObserverSourceName = nameof(DiagnosticSourceLogging) + "." + nameof(EventObserver);
    }
}