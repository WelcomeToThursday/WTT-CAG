namespace WTT_CAG.Client.Core;

// Info and debug lines only in a debug build; warnings and errors always.
public static class CagLog
{
    public static void Info(object message)
    {
        if (BuildFlags.DebugBuild) { Plugin.logger?.LogInfo(message); }
    }

    public static void Debug(object message)
    {
        if (BuildFlags.DebugBuild) { Plugin.logger?.LogDebug(message); }
    }

    public static void Warning(object message) => Plugin.logger?.LogWarning(message);

    public static void Error(object message) => Plugin.logger?.LogError(message);
}
