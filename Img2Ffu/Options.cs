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
using CommandLine;

namespace Img2Ffu
{
    internal class Options
    {
        [Option('i', "img-file", HelpText = @"A path to the img file to convert *OR* a PhysicalDisk path. i.e. \\.\PhysicalDrive1", Required = true)]
        public required string InputFile
        {
            get; set;
        }

        [Option('f', "ffu-file", HelpText = "A path to the FFU file to output", Required = true)]
        public required string OutputFFUFile
        {
            get; set;
        }

        [Option('e', "excluded-file", HelpText = "A path to the file with all partitions to exclude", Required = false, Default = ".\\provisioning-partitions.txt")]
        public required string ExcludedPartitionNamesFilePath
        {
            get; set;
        }

        [Option('p', "plat-id", HelpText = "Platform ID to use", Required = true)]
        public required string PlatformID
        {
            get; set;
        }

        [Option('a', "anti-theft-version", Required = false, HelpText = "Anti theft version.", Default = "1.1")]
        public required string AntiTheftVersion
        {
            get; set;
        }

        [Option('o', "os-version", Required = false, HelpText = "Operating system version.", Default = "10.0.11111.0")]
        public required string OperatingSystemVersion
        {
            get; set;
        }

        [Option('c', "block-size", Required = false, HelpText = "Block size to use for the FFU file", Default = 0x20000u)]
        public uint BlockSize
        {
            get; set;
        }

        [Option('s', "sector-size", Required = false, HelpText = "Sector size to use for the FFU file", Default = 0x200u)]
        public uint SectorSize
        {
            get; set;
        }

        [Option('b', "blanksectorbuffer-size", Required = false, HelpText = "Buffer size for the upper maximum allowed limit of blank sectors", Default = 100u)]
        public uint MaximumNumberOfBlankBlocksAllowed
        {
            get; set;
        }
    }
}