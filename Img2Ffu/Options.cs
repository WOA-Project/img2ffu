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
using System;

namespace Img2Ffu
{
    partial class Program
    {
        internal class Options
        {
            [Option('i', "img-file", HelpText = @"A path to the img file to convert *OR* a PhysicalDisk path. i.e. \\.\PhysicalDrive1", Required = true)]
            public string InputFile { get; set; }

            [Option('f', "ffu-file", HelpText = "A path to the FFU file to output", Required = true)]
            public string FFUFile { get; set; }

            [Option('e', "excluded-file", HelpText = "A path to the file with all partitions to exclude", Required = false, Default = ".\\provisioning-partitions.txt")]
            public string ExcludedPartitionNamesFilePath { get; set; }

            [Option('p', "plat-id", HelpText = "Platform ID to use", Required = true)]
            public string PlatformID { get; set; }

            [Option('a', "anti-theft-version", Required = false, HelpText = "Anti theft version.", Default = "1.1")]
            public string AntiTheftVersion { get; set; }

            [Option('o', "os-version", Required = false, HelpText = "Operating system version.", Default = "10.0.11111.0")]
            public string OperatingSystemVersion { get; set; }

            [Option('c', "block-size", Required = false, HelpText = "Block size to use for the FFU file", Default = 0x20000u)]
            public UInt32 BlockSize { get; set; }

            [Option('s', "sector-size", Required = false, HelpText = "Sector size to use for the FFU file", Default = 0x200u)]
            public UInt32 SectorSize { get; set; }

            [Option('b', "blanksectorbuffer-size", Required = false, HelpText = "Buffer size for the upper maximum allowed limit of blank sectors", Default = 100u)]
            public UInt32 MaximumNumberOfBlankBlocksAllowed { get; set; }
        }
    }
}