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
using DiscUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Img2Ffu
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                Logging.Log("img2ffu - Converts raw image (img) files into full flash update (FFU) files");
                Logging.Log("Copyright (c) 2019-2021, Gustave Monce - gus33000.me - @gus33000");
                Logging.Log("Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda");
                Logging.Log("Released under the MIT license at github.com/gus33000/img2ffu");
                Logging.Log("");

                try
                {
                    string filepath = o.ExcludedFile;

                    if (!File.Exists(filepath))
                    {
                        filepath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), o.ExcludedFile);
                    }

                    if (!File.Exists(filepath))
                    {
                        Logging.Log("Something happened.", Logging.LoggingLevel.Error);
                        Logging.Log("We couldn't find the provisioning partition file.", Logging.LoggingLevel.Error);
                        Logging.Log("Please specify one using the corresponding argument switch", Logging.LoggingLevel.Error);
                        Environment.Exit(1);
                        return;
                    }

                    GenerateFFU(o.ImgFile, o.FfuFile, o.PlatId, o.ChunkSize, o.Antitheftver, o.Osversion, File.ReadAllLines(filepath), o.BlankSectorBufferSize);
                }
                catch (Exception ex)
                {
                    Logging.Log("Something happened.", Logging.LoggingLevel.Error);
                    Logging.Log(ex.Message, Logging.LoggingLevel.Error);
                    Logging.Log(ex.StackTrace, Logging.LoggingLevel.Error);
                    Environment.Exit(1);
                }
            });
        }

        private static byte[] GenerateCatalogFile(byte[] hashData)
        {
            byte[] catalog_first_part = new byte[] { 0x30, 0x82, 0x01, 0x44, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02, 0xA0, 0x82, 0x01, 0x35, 0x30, 0x82, 0x01, 0x31, 0x02, 0x01, 0x01, 0x31, 0x00, 0x30, 0x82, 0x01, 0x26, 0x06, 0x09, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0A, 0x01, 0xA0, 0x82, 0x01, 0x17, 0x30, 0x82, 0x01, 0x13, 0x30, 0x0C, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x01, 0x01, 0x04, 0x10, 0xA8, 0xCA, 0xD9, 0x7D, 0xBF, 0x6D, 0x67, 0x4D, 0xB1, 0x4D, 0x62, 0xFB, 0xE6, 0x26, 0x22, 0xD4, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x31, 0x31, 0x30, 0x31, 0x32, 0x31, 0x32, 0x32, 0x37, 0x5A, 0x30, 0x0E, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x01, 0x02, 0x05, 0x00, 0x30, 0x81, 0xD1, 0x30, 0x81, 0xCE, 0x04, 0x1E, 0x48, 0x00, 0x61, 0x00, 0x73, 0x00, 0x68, 0x00, 0x54, 0x00, 0x61, 0x00, 0x62, 0x00, 0x6C, 0x00, 0x65, 0x00, 0x2E, 0x00, 0x62, 0x00, 0x6C, 0x00, 0x6F, 0x00, 0x62, 0x00, 0x00, 0x00, 0x31, 0x81, 0xAB, 0x30, 0x45, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x01, 0x04, 0x31, 0x37, 0x30, 0x35, 0x30, 0x10, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x01, 0x19, 0xA2, 0x02, 0x80, 0x00, 0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00, 0x04, 0x14 };
            byte[] catalog_second_part = new byte[] { 0x30, 0x62, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x02, 0x02, 0x31, 0x54, 0x30, 0x52, 0x1E, 0x4C, 0x00, 0x7B, 0x00, 0x44, 0x00, 0x45, 0x00, 0x33, 0x00, 0x35, 0x00, 0x31, 0x00, 0x41, 0x00, 0x34, 0x00, 0x32, 0x00, 0x2D, 0x00, 0x38, 0x00, 0x45, 0x00, 0x35, 0x00, 0x39, 0x00, 0x2D, 0x00, 0x31, 0x00, 0x31, 0x00, 0x44, 0x00, 0x30, 0x00, 0x2D, 0x00, 0x38, 0x00, 0x43, 0x00, 0x34, 0x00, 0x37, 0x00, 0x2D, 0x00, 0x30, 0x00, 0x30, 0x00, 0x43, 0x00, 0x30, 0x00, 0x34, 0x00, 0x46, 0x00, 0x43, 0x00, 0x32, 0x00, 0x39, 0x00, 0x35, 0x00, 0x45, 0x00, 0x45, 0x00, 0x7D, 0x02, 0x02, 0x02, 0x00, 0x31, 0x00 };

            byte[] hash = new SHA1Managed().ComputeHash(hashData);

            byte[] catalog = new byte[catalog_first_part.Length + hash.Length + catalog_second_part.Length];
            Buffer.BlockCopy(catalog_first_part, 0, catalog, 0, catalog_first_part.Length);
            Buffer.BlockCopy(hash, 0, catalog, catalog_first_part.Length, hash.Length);
            Buffer.BlockCopy(catalog_second_part, 0, catalog, catalog_first_part.Length + hash.Length, catalog_second_part.Length);

            return catalog;
        }

        private static byte[] GetResultingBuffer(IEnumerable<FlashingPayload> payloads)
        {
            UInt32 WriteDescriptorLength = 0;
            foreach (FlashingPayload payload in payloads)
            {
                WriteDescriptorLength += payload.GetStoreHeaderSize();
            }

            byte[] descriptorsBuffer = new byte[WriteDescriptorLength];

            UInt32 NewWriteDescriptorOffset = 0;
            foreach (FlashingPayload payload in payloads)
            {
                var descriptor = payload.GetFlashingPayloadDescriptor();

                ByteOperations.WriteUInt32(descriptorsBuffer, NewWriteDescriptorOffset + 0x00, descriptor.LocationCount); // Location count
                ByteOperations.WriteUInt32(descriptorsBuffer, NewWriteDescriptorOffset + 0x04, descriptor.ChunkCount);    // Chunk count
                NewWriteDescriptorOffset += 0x08;

                foreach (var dataDescriptor in descriptor.flashingPayloadDataDescriptors)
                {
                    ByteOperations.WriteUInt32(descriptorsBuffer, NewWriteDescriptorOffset + 0x00, dataDescriptor.DiskAccessMethod); // Disk access method (0 = Begin, 2 = End)
                    ByteOperations.WriteUInt32(descriptorsBuffer, NewWriteDescriptorOffset + 0x04, dataDescriptor.ChunkIndex);       // Chunk index
                    NewWriteDescriptorOffset += 0x08;
                }
            }
            return descriptorsBuffer;
        }

        private static void GenerateFFU(string ImageFile, string FFUFile, string PlatformId, UInt32 chunkSize, string AntiTheftVersion, string Osversion, string[] excluded, UInt32 BlankSectorBufferSize)
        {
            Logging.Log("Input image: " + ImageFile);
            Logging.Log("Destination image: " + FFUFile);
            Logging.Log("Platform ID: " + PlatformId);
            Logging.Log("");

            Stream stream;
            VirtualDisk destDisk = null;

            if (ImageFile.ToLower().Contains(@"\\.\physicaldrive"))
            {
                stream = new DeviceStream(ImageFile, FileAccess.Read);
            }
            else if (File.Exists(ImageFile) && Path.GetExtension(ImageFile).ToLowerInvariant() == ".vhd")
            {
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhd.Disk).Assembly);
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhdx.Disk).Assembly);
                destDisk = VirtualDisk.OpenDisk(ImageFile, FileAccess.Read);
                stream = destDisk.Content;
            }
            else if (File.Exists(ImageFile) && Path.GetExtension(ImageFile).ToLowerInvariant() == ".vhdx")
            {
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhd.Disk).Assembly);
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhdx.Disk).Assembly);
                destDisk = VirtualDisk.OpenDisk(ImageFile, FileAccess.Read);
                stream = destDisk.Content;
            }
            else if (File.Exists(ImageFile))
            {
                stream = new FileStream(ImageFile, FileMode.Open);
            }
            else
            {
                Logging.Log("Unknown input specified");
                return;
            }

            (FlashPart[] flashParts, ulong PlatEnd, List<GPT.Partition> partitions) = ImageSplitter.GetImageSlices(stream, chunkSize, excluded);

            IOrderedEnumerable<FlashingPayload> payloads = FlashingPayloadGenerator.GetOptimizedPayloads(flashParts, chunkSize, BlankSectorBufferSize).OrderBy(x => x.TargetLocations.First());

            Logging.Log("");
            Logging.Log("Building image headers...");

            string header1 = Path.GetTempFileName();
            FileStream Headerstream1 = new FileStream(header1, FileMode.OpenOrCreate);

            // ==============================
            // Header 1 start

            ImageHeader image = new ImageHeader();
            FullFlash ffimage = new FullFlash();
            Store simage = new Store();

            // Todo make this read the image itself
            ffimage.OSVersion = Osversion;
            ffimage.DevicePlatformId0 = PlatformId;
            ffimage.AntiTheftVersion = AntiTheftVersion;

            simage.SectorSize = 512;
            simage.MinSectorCount = (UInt32)(stream.Length / 512);

            Logging.Log("Generating image manifest...");
            string manifest = ManifestIni.BuildUpManifest(ffimage, simage, partitions);

            byte[] TextBytes = System.Text.Encoding.ASCII.GetBytes(manifest);

            image.ManifestLength = (UInt32)TextBytes.Length;

            byte[] ImageHeaderBuffer = new byte[0x18];

            ByteOperations.WriteUInt32(ImageHeaderBuffer, 0, image.Size);
            ByteOperations.WriteAsciiString(ImageHeaderBuffer, 0x04, image.Signature);
            ByteOperations.WriteUInt32(ImageHeaderBuffer, 0x10, image.ManifestLength);
            ByteOperations.WriteUInt32(ImageHeaderBuffer, 0x14, image.ChunkSize);

            Headerstream1.Write(ImageHeaderBuffer, 0, 0x18);
            Headerstream1.Write(TextBytes, 0, TextBytes.Length);

            RoundUpToChunks(Headerstream1, chunkSize);

            // Header 1 stop + round
            // ==============================

            string header2 = Path.GetTempFileName();
            FileStream Headerstream2 = new FileStream(header2, FileMode.OpenOrCreate);

            // ==============================
            // Header 2 start

            StoreHeader store = new StoreHeader();

            store.WriteDescriptorCount = (UInt32)payloads.Count();
            store.FinalTableIndex = (UInt32)payloads.Count() - store.FinalTableCount;
            store.PlatformId = PlatformId;

            byte[] WriteDescriptorBuffer = GetResultingBuffer(payloads);
            store.WriteDescriptorLength = (UInt32)WriteDescriptorBuffer.Length;

            foreach (FlashingPayload payload in payloads)
            {
                if (payload.TargetLocations.First() > PlatEnd)
                    break;
                store.FlashOnlyTableIndex += 1;
            }

            Headerstream2.Write(store.GetResultingBuffer(), 0, 0xF8);
            Headerstream2.Write(WriteDescriptorBuffer, 0, (Int32)store.WriteDescriptorLength);

            RoundUpToChunks(Headerstream2, chunkSize);

            // Header 2 stop + round
            // ==============================

            SecurityHeader security = new SecurityHeader();

            Headerstream1.Seek(0, SeekOrigin.Begin);
            Headerstream2.Seek(0, SeekOrigin.Begin);

            security.HashTableSize = 0x20 * (UInt32)((Headerstream1.Length + Headerstream2.Length) / chunkSize);

            foreach (FlashingPayload payload in payloads)
            {
                security.HashTableSize += payload.GetSecurityHeaderSize();
            }

            byte[] HashTable = new byte[security.HashTableSize];
            BinaryWriter bw = new BinaryWriter(new MemoryStream(HashTable));

            SHA256 crypto = SHA256.Create();
            for (int i = 0; i < Headerstream1.Length / chunkSize; i++)
            {
                byte[] buffer = new byte[chunkSize];
                Headerstream1.Read(buffer, 0, (Int32)chunkSize);
                byte[] hash = crypto.ComputeHash(buffer);
                bw.Write(hash, 0, hash.Length);
            }

            for (int i = 0; i < Headerstream2.Length / chunkSize; i++)
            {
                byte[] buffer = new byte[chunkSize];
                Headerstream2.Read(buffer, 0, (Int32)chunkSize);
                byte[] hash = crypto.ComputeHash(buffer);
                bw.Write(hash, 0, hash.Length);
            }

            foreach (FlashingPayload payload in payloads)
            {
                foreach (var chunkHash in payload.ChunkHashes)
                {
                    bw.Write(chunkHash, 0, chunkHash.Length);
                }
            }

            bw.Close();

            Logging.Log("Generating image catalog...");
            byte[] catalog = GenerateCatalogFile(HashTable);

            security.CatalogSize = (UInt32)catalog.Length;

            byte[] SecurityHeaderBuffer = new byte[0x20];

            ByteOperations.WriteUInt32(SecurityHeaderBuffer, 0, security.Size);
            ByteOperations.WriteAsciiString(SecurityHeaderBuffer, 0x04, security.Signature);
            ByteOperations.WriteUInt32(SecurityHeaderBuffer, 0x10, security.ChunkSizeInKb);
            ByteOperations.WriteUInt32(SecurityHeaderBuffer, 0x14, security.HashAlgorithm);
            ByteOperations.WriteUInt32(SecurityHeaderBuffer, 0x18, security.CatalogSize);
            ByteOperations.WriteUInt32(SecurityHeaderBuffer, 0x1C, security.HashTableSize);

            FileStream retstream = new FileStream(FFUFile, FileMode.CreateNew);

            retstream.Write(SecurityHeaderBuffer, 0, 0x20);

            retstream.Write(catalog, 0, (Int32)security.CatalogSize);
            retstream.Write(HashTable, 0, (Int32)security.HashTableSize);

            RoundUpToChunks(retstream, chunkSize);

            Headerstream1.Seek(0, SeekOrigin.Begin);
            Headerstream2.Seek(0, SeekOrigin.Begin);

            byte[] buff = new byte[Headerstream1.Length];
            Headerstream1.Read(buff, 0, (Int32)Headerstream1.Length);

            Headerstream1.Close();
            File.Delete(header1);

            retstream.Write(buff, 0, buff.Length);

            buff = new byte[Headerstream2.Length];
            Headerstream2.Read(buff, 0, (Int32)Headerstream2.Length);

            Headerstream2.Close();
            File.Delete(header2);

            retstream.Write(buff, 0, buff.Length);

            Logging.Log("Writing payloads...");
            UInt64 counter = 0;

            DateTime startTime = DateTime.Now;

            foreach (FlashingPayload payload in payloads)
            {
                for (int i = 0; i < payload.StreamIndexes.Length; i++)
                {
                    UInt32 StreamIndex = payload.StreamIndexes[i];
                    FlashPart flashPart = flashParts[StreamIndex];
                    Stream Stream = flashPart.Stream;
                    Stream.Seek(payload.StreamLocations[i], SeekOrigin.Begin);

                    byte[] buffer = new byte[chunkSize];
                    Stream.Read(buffer, 0, (Int32)chunkSize);
                    retstream.Write(buffer, 0, (Int32)chunkSize);
                    counter++;

                    ShowProgress((UInt64)payloads.Count() * chunkSize, startTime, counter * chunkSize, counter * chunkSize, payload.TargetLocations[i] * chunkSize < PlatEnd);
                }
            }

            retstream.Close();
            if (destDisk != null)
                destDisk.Dispose();
            Logging.Log("");
        }

        private static void RoundUpToChunks(Stream stream, UInt32 chunkSize)
        {
            Int64 Size = stream.Length;
            if ((Size % chunkSize) > 0)
            {
                Int64 padding = (UInt32)(((Size / chunkSize) + 1) * chunkSize) - Size;
                stream.Write(new byte[padding], 0, (Int32)padding);
            }
        }

        private static void ShowProgress(ulong totalBytes, DateTime startTime, ulong BytesRead, ulong SourcePosition, bool DisplayRed)
        {
            var now = DateTime.Now;
            var timeSoFar = now - startTime;

            var remaining = TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / BytesRead * (totalBytes - BytesRead));

            var speed = Math.Round(SourcePosition / 1024L / 1024L / timeSoFar.TotalSeconds);

            Logging.Log(string.Format("{0} {1}MB/s {2:hh\\:mm\\:ss\\.f}", GetDismLikeProgBar(int.Parse((BytesRead * 100 / totalBytes).ToString())), speed.ToString(), remaining, remaining.TotalHours, remaining.Minutes, remaining.Seconds, remaining.Milliseconds), returnline: false, severity: DisplayRed ? Logging.LoggingLevel.Warning : Logging.LoggingLevel.Information);
        }

        private static string GetDismLikeProgBar(int perc)
        {
            var eqsLength = (int)((double)perc / 100 * 55);
            var bases = new string('=', eqsLength) + new string(' ', 55 - eqsLength);
            bases = bases.Insert(28, perc + "%");
            if (perc == 100)
                bases = bases.Substring(1);
            else if (perc < 10)
                bases = bases.Insert(28, " ");
            return "[" + bases + "]";
        }

        internal class Options
        {
            [Option('i', "img-file", HelpText = @"A path to the img file to convert *OR* a PhysicalDisk path. i.e. \\.\PhysicalDrive1", Required = true)]
            public string ImgFile { get; set; }

            [Option('f', "ffu-file", HelpText = "A path to the FFU file to output", Required = true)]
            public string FfuFile { get; set; }

            [Option('e', "excluded-file", HelpText = "A path to the file with all partitions to exclude", Required = false, Default = ".\\provisioning-partitions.txt")]
            public string ExcludedFile { get; set; }

            [Option('p', "plat-id", HelpText = "Platform ID to use", Required = true)]
            public string PlatId { get; set; }

            [Option('a', "anti-theft-version", Required = false, HelpText = "Anti theft version.", Default = "1.1")]
            public string Antitheftver { get; set; }

            [Option('o', "os-version", Required = false, HelpText = "Operating system version.", Default = "10.0.11111.0")]
            public string Osversion { get; set; }

            [Option('c', "chunk-size", Required = false, HelpText = "Chunk size to use for the FFU file", Default = 131072u)]
            public UInt32 ChunkSize { get; set; }

            [Option('b', "blanksectorbuffer-size", Required = false, HelpText = "Buffer size for the upper maximum allowed limit of blank sectors", Default = 100u)]
            public UInt32 BlankSectorBufferSize { get; set; }
        }
    }
}