namespace STIN_Burza.Services
{
    public interface IMyLogger
    {
        void Log(string message);
        List<string> GetLastLines(int lineCount = 100);
    }
}
