using System;
using System.IO;
using System.Runtime.ExceptionServices;
using GromCore.Laser.Server.Database;
using System.Threading.Tasks;

public static class ErrorHandler
{
    private static string LogFilePath = "errors.txt";

    public static void Initialize()
    {
        Console.WriteLine("[ErrorHandler] ErrorHandler started!");
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
    }

    private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
    {
        LogException(e.Exception, "First-chance exception");
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex, "Unhandled exception");
        }
        else
        {
            File.AppendAllText(LogFilePath, $"[Time] {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n[Context] Critical error\n[Message] {e.ExceptionObject}\n----------------------------------------\n");
        }
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        foreach (var ex in e.Exception.InnerExceptions)
        {
            LogException(ex, "Unobserved task exception");
        }
    }

    private async static void LogException(Exception ex, string context)
    {
        static string GetFullStackTrace(Exception ex)
        {
            var st = new System.Text.StringBuilder();
            while (ex != null)
            {
                st.AppendLine($"--- Exception: {ex.GetType().FullName} ---");
                st.AppendLine($"Message: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    st.AppendLine("Stack Trace:");
                    string[] lines = ex.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        st.AppendLine($"  {line}");
                    }
                }
                else
                {
                    st.AppendLine("  [No Stack Trace Available]");
                }
                ex = ex.InnerException;
                if (ex != null)
                {
                    st.AppendLine(); 
                }
            }
            return st.ToString();
        }

        try
        {
            string logEntry = $@"
[Time] {DateTime.Now:yyyy-MM-dd HH:mm:ss}
[Context] {context}
[Message] {ex.Message}
[Type] {ex.GetType().FullName}
[Source] {ex.Source}
[StackTrace] {GetFullStackTrace(ex)}
----------------------------------------
";

            File.AppendAllText(LogFilePath, logEntry);

            try
            {
                
                //await TelegramBot.SendErrorToAdmins(ex, context);
            }
            catch(Exception ex1)
            {
                Console.WriteLine($"Error: {ex1.Message}");
            }
        }
        catch (Exception innerEx)
        {
            Console.WriteLine($"[ErrorHandler] не удалось залогировать ошибку: {innerEx.Message}");
        }
    }

}