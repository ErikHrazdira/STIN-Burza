namespace STIN_Burza.Services
{
    public class Logger
    {
        private readonly string logFilePath;

        public Logger(string logFilePath)
        {
            this.logFilePath = logFilePath;
        }

        // Přidání logu
        public void Log(string message)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllLines(logFilePath, new[] { logMessage });
        }

        public List<string> GetLastLines(int lineCount = 20)
        {
            if (!File.Exists(logFilePath))
                return new List<string>();

            var lines = File.ReadLines(logFilePath).Reverse().Take(lineCount).ToList();
            return lines;
        }
    }
}
