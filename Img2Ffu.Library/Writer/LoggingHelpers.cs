/*
 * Copyright (c) Gustave Monce and Contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
namespace Img2Ffu.Writer
{
    internal static class LoggingHelpers
    {
        private static string GetDISMLikeProgressBar(uint percentage)
        {
            if (percentage > 100)
            {
                percentage = 100;
            }

            int eqsLength = (int)Math.Floor((double)percentage * 55u / 100u);

            string bases = $"{new string('=', eqsLength)}{new string(' ', 55 - eqsLength)}";

            bases = bases.Insert(28, percentage + "%");

            if (percentage == 100)
            {
                bases = bases[1..];
            }
            else if (percentage < 10)
            {
                bases = bases.Insert(28, " ");
            }

            return $"[{bases}]";
        }


        internal static void ShowProgress(ulong CurrentProgress,
                                        ulong TotalProgress,
                                        DateTime startTime,
                                        bool DisplayRed,
                                        ILogging Logging)
        {
            uint ProgressPercentage = TotalProgress == 0 ? 100 : (uint)(CurrentProgress * 100 / TotalProgress);

            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = new(0);

            double milliSecondsRemaining;
            if ((TotalProgress - CurrentProgress) == 0)
            {
                milliSecondsRemaining = 0;
            }
            else
            {
                milliSecondsRemaining = (double)(timeSoFar.TotalMilliseconds / CurrentProgress * (TotalProgress - CurrentProgress));
            }

            try
            {
                remaining = TimeSpan.FromMilliseconds(milliSecondsRemaining);
            }
            catch { }

            ILoggingLevel level;

            if (DisplayRed)
            {
                level = ILoggingLevel.Warning;
            }
            else
            {
                level = ILoggingLevel.Information;
            }

            Logging.Log($"{GetDISMLikeProgressBar(ProgressPercentage)} {remaining:hh\\:mm\\:ss\\.f}", severity: level, returnLine: false);
        }

        internal static void ShowProgress(ulong TotalBytes,
                                          ulong BytesRead,
                                          ulong SourcePosition,
                                          DateTime startTime,
                                          ILogging Logging)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = new(0);

            double milliSecondsRemaining;
            if ((TotalBytes - BytesRead) == 0)
            {
                milliSecondsRemaining = 0;
            }
            else
            {
                milliSecondsRemaining = (double)(timeSoFar.TotalMilliseconds / BytesRead * (TotalBytes - BytesRead));
            }

            try
            {
                remaining = TimeSpan.FromMilliseconds(milliSecondsRemaining);
            }
            catch { }

            double speed = Math.Round(SourcePosition / 1024L / 1024L / timeSoFar.TotalSeconds);

            Logging.Log(
                $"{GetDISMLikeProgressBar(uint.Parse((BytesRead * 100 / TotalBytes).ToString()))} {speed}MB/s {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}",
                returnLine: false,
                severity: ILoggingLevel.Information);
        }
    }
}
