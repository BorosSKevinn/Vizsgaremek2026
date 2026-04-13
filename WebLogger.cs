namespace Vizsgaremek2026
{
    public class WebLogger
    {
        public static void Log(string message)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "web_logs.txt");
            try
            {
                File.AppendAllText(logPath, $"{DateTime.UtcNow}: {message}\n");
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(AppContext.BaseDirectory, "web_errors.txt");
                File.AppendAllText(errorPath, $"{DateTime.UtcNow}: FILE ERROR: {ex.Message}\n");
            }
        }
    }
}
