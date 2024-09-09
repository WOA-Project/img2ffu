namespace Img2Ffu.Writer
{
    public interface ILogging
    {
        public void Log(string message, ILoggingLevel severity = ILoggingLevel.Information, bool returnLine = true);
    }
}