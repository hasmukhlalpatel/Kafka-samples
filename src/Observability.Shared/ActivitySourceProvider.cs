using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Observability.Shared;

public sealed class ActivitySourceProvider
{
    private ActivitySourceProvider() { }
    public void Initialize(string activitySourceName)
    {
        if (string.IsNullOrWhiteSpace(activitySourceName))
            throw new ArgumentException("Activity source name cannot be null or whitespace.", nameof(activitySourceName));

        if (ActivitySource != null)
            throw new InvalidOperationException("ActivitySourceProvider has already been initialized.");

        ActivitySource = new ActivitySource(activitySourceName);
    }

    public static ActivitySourceProvider Instance { get; private set; } = new ActivitySourceProvider();
    public ActivitySource ActivitySource { get; private set; }
    public Activity? GetActivity() => Activity.Current;

    public Activity? StartClientActivity(string name) => StartActivity(name, kind: ActivityKind.Client);
    public Activity? StartServerActivity(string name) => StartActivity(name, kind: ActivityKind.Server);
    public Activity? StartInternalActivity(string name) => StartActivity(name, kind: ActivityKind.Internal);
    public Activity? StartProducerActivity(string name) => StartActivity(name, kind: ActivityKind.Producer);
    public Activity? StartConsumerActivity(string name) => StartActivity(name, kind: ActivityKind.Consumer);
    public Activity? StartActivity([CallerMemberName] string name = "", ActivityKind kind = ActivityKind.Internal)
    {
        return ActivitySource?.StartActivity(name, kind: kind);
    }
}
