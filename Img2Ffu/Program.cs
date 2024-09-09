/*

Copyright (c) 2019, Gustave Monce - gus33000.me - @gus33000

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
using CommandLine;
using Img2Ffu.Writer;
using Img2Ffu.Writer.Data;
using System;
using System.IO;
using System.Reflection;

namespace Img2Ffu
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            _ = Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                Logging.Log("img2ffu - Converts raw image (img) files into full flash update (FFU) files");
                Logging.Log("Copyright (c) 2019-2024, Gustave Monce - gus33000.me - @gus33000");
                Logging.Log("Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda");
                Logging.Log("Released under the MIT license at github.com/WOA-Project/img2ffu");
                Logging.Log("");

                try
                {
                    string ExcludedPartitionNamesFilePath = o.ExcludedPartitionNamesFilePath;

                    if (!File.Exists(ExcludedPartitionNamesFilePath))
                    {
                        ExcludedPartitionNamesFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), o.ExcludedPartitionNamesFilePath);
                    }

                    if (!File.Exists(ExcludedPartitionNamesFilePath))
                    {
                        Logging.Log("Something happened.", LoggingLevel.Error);
                        Logging.Log("We couldn't find the provisioning partition file.", LoggingLevel.Error);
                        Logging.Log("Please specify one using the corresponding argument switch", LoggingLevel.Error);
                        Environment.Exit(1);
                        return;
                    }

                    FFUFactory.GenerateFFU(o.InputFile, o.FFUFile, o.PlatformID.Split(';'), o.SectorSize, o.BlockSize, o.AntiTheftVersion, o.OperatingSystemVersion, File.ReadAllLines(ExcludedPartitionNamesFilePath), o.MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion.V2, [], new LoggingImplementation());;
                }
                catch (Exception ex)
                {
                    Logging.Log("Something happened.", LoggingLevel.Error);
                    Logging.Log(ex.Message, LoggingLevel.Error);
                    Logging.Log(ex.StackTrace, LoggingLevel.Error);
                    Environment.Exit(1);
                }
            });
        }
    }
}