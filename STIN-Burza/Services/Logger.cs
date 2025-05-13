namespace STIN_Burza.Services
{
    public class Logger(string logFilePath)
    {
        private readonly string logFilePath = logFilePath;

        // Přidání logu
        public void Log(string message)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllLines(logFilePath, [logMessage]);
        }

        public List<string> GetLastLines(int lineCount = 100)
        {
            if (!File.Exists(logFilePath))
                return [];

            var lines = File.ReadLines(logFilePath).Reverse().Take(lineCount).Reverse().ToList();
            return lines;
        }
    }
}
