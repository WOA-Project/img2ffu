using Img2Ffu.Writer;
using System.Collections.Generic;

namespace Img2Ffu
{
    internal class LoggingImplementation : ILogging
    {
        private readonly Dictionary<ILoggingLevel, LoggingLevel> levelTransform = new()
        {
            {
                ILoggingLevel.Information, 
                LoggingLevel.Information
            },
            {
                ILoggingLevel.Warning,
                LoggingLevel.Warning
            },
            {
                ILoggingLevel.Error,
                LoggingLevel.Error
            },
        };

        public void Log(string message, ILoggingLevel severity = ILoggingLevel.Information, bool returnLine = true)
        {
            Logging.Log(message, levelTransform[severity], returnLine);
        }
    }
}
