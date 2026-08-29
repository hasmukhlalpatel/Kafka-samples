namespace Observability.Shared;

public static class LogicalCallContext
{
    public static class LoggingConstants
    {
        public const string CorrelationId = "CorrelationId";
        public const string XCorrelationId = "X-Correlation-Id";
        public const string AppLoggerContext = "AppLoggerContext";
        public const string LogicalCallContext = "X-Context";
    }

    public static class Constants
    {
        public const string CorrelationIdSource = "CorrelationIdSource";
        public const string CorrelationId = "CorrelationId";
        public const string XCorrelationId = "X-Correlation-Id";
        public const string SourceMachine = "SourceMachine";
        public const string UserId = "UserId";
        public const string LogicalCallContext = "X-Context";
    }
}
public class LogicalCallContext<T> : IDisposable
//where T : class, new()
{
    private static readonly AsyncLocal<Stack<T>> _asyncLocal = new AsyncLocal<Stack<T>>();

    public LogicalCallContext(T t)
    {
        AsyncLocal<Stack<T>> asyncLocal = LogicalCallContext<T>._asyncLocal;
        if (asyncLocal.Value == null)
        {
            Stack<T> objStack;
            asyncLocal.Value = objStack = new Stack<T>();
        }
        LogicalCallContext<T>._asyncLocal.Value.Push(t);
    }

    public static T Current
    {
        get
        {
            Stack<T> objStack = LogicalCallContext<T>._asyncLocal.Value;
            return objStack == null || objStack.Count <= 0 ? default(T) : objStack.Peek();

        }
    }

    public static IReadOnlyCollection<T> ContextValues => (IReadOnlyCollection<T>)LogicalCallContext<T>._asyncLocal.Value ?? (IReadOnlyCollection<T>)new Stack<T>();

    public void Dispose()
    {
        LogicalCallContext<T>._asyncLocal.Value?.Pop();
        GC.SuppressFinalize((object)this);
    }
}

public class ApplicationContext
{
    public ApplicationContext(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
    public ApplicationContext()
    {
        
    }
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

}

public class ApplicationContextScope : LogicalCallContext<ApplicationContext>
{
    public ApplicationContextScope() : base(new ApplicationContext()) { }
    public ApplicationContextScope(Guid correlationId) : base(new ApplicationContext(correlationId)) { }

    public ApplicationContextScope(ApplicationContext applicationContext) : base(applicationContext)
    {
    }
}

