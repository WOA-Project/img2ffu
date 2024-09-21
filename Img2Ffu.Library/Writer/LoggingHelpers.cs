namespace Img2Ffu.Writer
{
    internal static class LoggingHelpers
    {
        private static string GetDismLikeProgBar(int perc)
        {
            int eqsLength = (int)((double)perc / 100 * 55);
            string bases = new string('=', eqsLength) + new string(' ', 55 - eqsLength);
            bases = bases.Insert(28, perc + "%");
            if (perc == 100)
            {
                bases = bases[1..];
            }
            else if (perc < 10)
            {
                bases = bases.Insert(28, " ");
            }

            return $"[{bases}]";
        }

        internal static void ShowProgress(ulong CurrentProgress, ulong TotalProgress, DateTime startTime, bool DisplayRed, ILogging Logging)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            double milliseconds = timeSoFar.TotalMilliseconds / CurrentProgress * (TotalProgress - CurrentProgress);
            double ticks = milliseconds * TimeSpan.TicksPerMillisecond;
            if (ticks > long.MaxValue || ticks < long.MinValue || double.IsNaN(ticks))
            {
                milliseconds = 0;
            }
            TimeSpan remaining = TimeSpan.FromMilliseconds(milliseconds);

            Logging.Log(string.Format($"{LoggingHelpers.GetDismLikeProgBar(int.Parse((CurrentProgress * 100 / TotalProgress).ToString()))} {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}"), returnLine: false, severity: DisplayRed ? ILoggingLevel.Warning : ILoggingLevel.Information);
        }

        internal static void ShowProgress(ulong TotalBytes, ulong BytesRead, ulong SourcePosition, DateTime startTime, ILogging Logging)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            double milliseconds = timeSoFar.TotalMilliseconds / BytesRead * (TotalBytes - BytesRead);
            double ticks = milliseconds * TimeSpan.TicksPerMillisecond;
            if ((ticks > long.MaxValue) || (ticks < long.MinValue) || double.IsNaN(ticks))
            {
                milliseconds = 0;
            }
            TimeSpan remaining = TimeSpan.FromMilliseconds(milliseconds);

            double speed = Math.Round(SourcePosition / 1024L / 1024L / timeSoFar.TotalSeconds);

            Logging.Log(string.Format($"{GetDismLikeProgBar(int.Parse((BytesRead * 100 / TotalBytes).ToString()))} {speed}MB/s {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}"), returnLine: false, severity: ILoggingLevel.Information);
        }
    }
}
