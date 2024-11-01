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
using Img2Ffu.Writer.Data;
using System.Collections.Generic;

namespace Img2Ffu
{
    internal class Options : StoreInputOptions
    {
        [Option('f', "ffu-file", HelpText = "A path to the FFU file to output", Required = true)]
        public required string OutputFFUFile
        {
            get; set;
        }

        [Option('p', "plat-id", HelpText = "Platform ID to use", Separator = ';', Required = true)]
        public required IEnumerable<string> PlatformIDs
        {
            get; set;
        }

        [Option('a', "anti-theft-version", HelpText = "Anti theft version.", Default = "1.1", Required = false)]
        public required string AntiTheftVersion
        {
            get; set;
        }

        [Option('o', "os-version", HelpText = "Operating system version.", Default = "10.0.11111.0", Required = false)]
        public required string OperatingSystemVersion
        {
            get; set;
        }

        [Option('c', "block-size", HelpText = "Block size to use for the FFU file", Default = 0x20000u, Required = false)]
        public uint BlockSize
        {
            get; set;
        }

        [Option('s', "sector-size", HelpText = "Sector size to use for the FFU file", Default = 0x200u, Required = false)]
        public uint SectorSize
        {
            get; set;
        }

        [Option('v', "full-flash-update-version", HelpText = "Version of the FFU file format to use, can be either V1 or V2", Default = FlashUpdateVersion.V1, Required = false)]
        public FlashUpdateVersion FlashUpdateVersion
        {
            get; set;
        }

        [Option('b', "secure-boot-signing-command", HelpText = "Optional command to sign the resulting FFU file to work in Secure Boot scenarios. The File to sign is passed as a path appended after the provided command string, and is the sole and only argument provided.", Default = "", Required = false)]
        public required string SecureBootSigningCommand
        {
            get; set;
        }
    }
}