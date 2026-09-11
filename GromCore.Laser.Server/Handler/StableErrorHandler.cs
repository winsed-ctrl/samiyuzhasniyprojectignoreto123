using System.Text;

namespace GromCore.Laser.Server.Handler;

internal static class StableErrorHandler
{
    private const long MaxLogSize = 10 * 1024 * 1024;
    private const string LogFilePath = "critical-errors.txt";
    private static readonly object LogLock = new();

    public static void Initialize()
    {
        Console.WriteLine("[ErrorHandler] Critical error logging started.");
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
        {
            LogException(exception, "Unhandled exception");
        }
        else
        {
            Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Unhandled error: {args.ExceptionObject}");
        }
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
    {
        LogException(args.Exception, "Unobserved task exception");
        args.SetObserved();
    }

    private static void LogException(Exception exception, string context)
    {
        StringBuilder entry = new();
        entry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}");

        for (Exception current = exception; current != null; current = current.InnerException)
        {
            entry.AppendLine($"{current.GetType().FullName}: {current.Message}");
            entry.AppendLine(current.StackTrace ?? "[No stack trace]");
        }

        Append(entry.ToString());
    }

    private static void Append(string entry)
    {
        try
        {
            lock (LogLock)
            {
                RotateIfNeeded();
                File.AppendAllText(LogFilePath, entry + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception logError)
        {
            Console.WriteLine($"[ErrorHandler] Failed to write critical error: {logError.Message}");
        }
    }

    private static void RotateIfNeeded()
    {
        FileInfo file = new(LogFilePath);
        if (!file.Exists || file.Length < MaxLogSize) return;

        string archivePath = $"critical-errors-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        File.Move(LogFilePath, archivePath);
    }
}
