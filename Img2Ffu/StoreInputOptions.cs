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
using System.Collections.Generic;

namespace Img2Ffu
{
    internal class StoreInputOptions
    {
        [Option('i', "img-file", Group = "StoreInputOptions", HelpText = @"A path to the img file to convert *OR* a PhysicalDisk path. i.e. \\.\PhysicalDrive1", Separator = ';', Required = true)]
        public required IEnumerable<string> InputFile
        {
            get; set;
        }

        [Option('d', "device-path", Group = "StoreInputOptions", HelpText = @"The UEFI device path to write the store onto when flashing.", Separator = ';', Default = new[] { "VenHw(B615F1F5-5088-43CD-809C-A16E52487D00)" }, Required = false)]
        public required IEnumerable<string> DevicePath
        {
            get; set;
        }

        [Option('l', "is-fixed-disk-length", Group = "StoreInputOptions", HelpText = @"Specifies the disk in question is fixed and cannot have a different size on the end user target device. Means every data part of this disk is required for correct device firmware operation and must not be half flashed before a valid GPT is available on disk.", Separator = ';', Default = new[] { true }, Required = false)]
        public required IEnumerable<bool> IsFixedDiskLength
        {
            get; set;
        }

        [Option('b', "blanksectorbuffer-size", Group = "StoreInputOptions", HelpText = "Buffer size for the upper maximum allowed limit of blank sectors", Separator = ';', Default = new[] { 100u }, Required = false)]
        public IEnumerable<uint> MaximumNumberOfBlankBlocksAllowed
        {
            get; set;
        }

        [Option('e', "excluded-file", Group = "StoreInputOptions", HelpText = "A path to the file with all partitions to exclude", Separator = ';', Default = new[] { ".\\provisioning-partitions.txt" }, Required = false)]
        public required IEnumerable<string> ExcludedPartitionNamesFilePath
        {
            get; set;
        }
    }
}