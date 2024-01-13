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
using Img2Ffu.Data;
using Img2Ffu.Flashing;
using Img2Ffu.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Img2Ffu
{
    partial class Program
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
                    string ExcludedPartitionNamesFilePath = o.ExcludedPartitionNamesFilePath;

                    if (!File.Exists(ExcludedPartitionNamesFilePath))
                    {
                        ExcludedPartitionNamesFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), o.ExcludedPartitionNamesFilePath);
                    }

                    if (!File.Exists(ExcludedPartitionNamesFilePath))
                    {
                        Logging.Log("Something happened.", Logging.LoggingLevel.Error);
                        Logging.Log("We couldn't find the provisioning partition file.", Logging.LoggingLevel.Error);
                        Logging.Log("Please specify one using the corresponding argument switch", Logging.LoggingLevel.Error);
                        Environment.Exit(1);
                        return;
                    }

                    GenerateFFU(o.InputFile, o.FFUFile, o.PlatformID, o.SectorSize, o.ChunkSize, o.AntiTheftVersion, o.OperatingSystemVersion, File.ReadAllLines(ExcludedPartitionNamesFilePath), o.MaximumNumberOfBlankBlocksAllowed);
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
            byte[] catalog_first_part = [0x30, 0x82, 0x01, 0x44, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02, 0xA0, 0x82, 0x01, 0x35, 0x30, 0x82, 0x01, 0x31, 0x02, 0x01, 0x01, 0x31, 0x00, 0x30, 0x82, 0x01, 0x26, 0x06, 0x09, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0A, 0x01, 0xA0, 0x82, 0x01, 0x17, 0x30, 0x82, 0x01, 0x13, 0x30, 0x0C, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x01, 0x01, 0x04, 0x10, 0xA8, 0xCA, 0xD9, 0x7D, 0xBF, 0x6D, 0x67, 0x4D, 0xB1, 0x4D, 0x62, 0xFB, 0xE6, 0x26, 0x22, 0xD4, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x31, 0x31, 0x30, 0x31, 0x32, 0x31, 0x32, 0x32, 0x37, 0x5A, 0x30, 0x0E, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x01, 0x02, 0x05, 0x00, 0x30, 0x81, 0xD1, 0x30, 0x81, 0xCE, 0x04, 0x1E, 0x48, 0x00, 0x61, 0x00, 0x73, 0x00, 0x68, 0x00, 0x54, 0x00, 0x61, 0x00, 0x62, 0x00, 0x6C, 0x00, 0x65, 0x00, 0x2E, 0x00, 0x62, 0x00, 0x6C, 0x00, 0x6F, 0x00, 0x62, 0x00, 0x00, 0x00, 0x31, 0x81, 0xAB, 0x30, 0x45, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x01, 0x04, 0x31, 0x37, 0x30, 0x35, 0x30, 0x10, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x01, 0x19, 0xA2, 0x02, 0x80, 0x00, 0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00, 0x04, 0x14];
            byte[] catalog_second_part = [0x30, 0x62, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x02, 0x02, 0x31, 0x54, 0x30, 0x52, 0x1E, 0x4C, 0x00, 0x7B, 0x00, 0x44, 0x00, 0x45, 0x00, 0x33, 0x00, 0x35, 0x00, 0x31, 0x00, 0x41, 0x00, 0x34, 0x00, 0x32, 0x00, 0x2D, 0x00, 0x38, 0x00, 0x45, 0x00, 0x35, 0x00, 0x39, 0x00, 0x2D, 0x00, 0x31, 0x00, 0x31, 0x00, 0x44, 0x00, 0x30, 0x00, 0x2D, 0x00, 0x38, 0x00, 0x43, 0x00, 0x34, 0x00, 0x37, 0x00, 0x2D, 0x00, 0x30, 0x00, 0x30, 0x00, 0x43, 0x00, 0x30, 0x00, 0x34, 0x00, 0x46, 0x00, 0x43, 0x00, 0x32, 0x00, 0x39, 0x00, 0x35, 0x00, 0x45, 0x00, 0x45, 0x00, 0x7D, 0x02, 0x02, 0x02, 0x00, 0x31, 0x00];

            byte[] hash = SHA1.HashData(hashData);

            byte[] catalog = new byte[catalog_first_part.Length + hash.Length + catalog_second_part.Length];
            Buffer.BlockCopy(catalog_first_part, 0, catalog, 0, catalog_first_part.Length);
            Buffer.BlockCopy(hash, 0, catalog, catalog_first_part.Length, hash.Length);
            Buffer.BlockCopy(catalog_second_part, 0, catalog, catalog_first_part.Length + hash.Length, catalog_second_part.Length);

            return catalog;
        }

        private static byte[] GetWriteDescriptorsBuffer(IEnumerable<FlashingPayload> payloads)
        {
            using MemoryStream WriteDescriptorsStream = new();
            BinaryWriter binaryWriter = new(WriteDescriptorsStream);

            foreach (FlashingPayload payload in payloads)
            {
                foreach (WriteDescriptor writeDescriptor in payload.WriteDescriptors)
                {
                    byte[] WriteDescriptorBuffer = writeDescriptor.GetResultingBuffer(StoreHeaderVersion.V1);
                    binaryWriter.Write(WriteDescriptorBuffer);
                }
            }

            byte[] WriteDescriptorsBuffer = new byte[WriteDescriptorsStream.Length];
            WriteDescriptorsStream.Seek(0, SeekOrigin.Begin);
            WriteDescriptorsStream.ReadExactly(WriteDescriptorsBuffer, 0, WriteDescriptorsBuffer.Length);

            return WriteDescriptorsBuffer;
        }

        private static void GenerateFFU(string InputFile, string FFUFile, string PlatformID, UInt32 SectorSize, UInt32 BlockSize, string AntiTheftVersion, string OperatingSystemVersion, string[] ExcludedPartitionNames, UInt32 MaximumNumberOfBlankBlocksAllowed)
        {
            Logging.Log("Input image: " + InputFile);
            Logging.Log("Destination image: " + FFUFile);
            Logging.Log("Platform ID: " + PlatformID);
            Logging.Log("");

            Stream stream;
            VirtualDisk inputDisk = null;

            if (InputFile.Contains(@"\\.\physicaldrive", StringComparison.CurrentCultureIgnoreCase))
            {
                stream = new Streams.DeviceStream(InputFile, FileAccess.Read);
            }
            else if (File.Exists(InputFile) && Path.GetExtension(InputFile).Equals(".vhd", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhd.Disk).Assembly);
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhdx.Disk).Assembly);
                inputDisk = VirtualDisk.OpenDisk(InputFile, FileAccess.Read);
                stream = inputDisk.Content;
            }
            else if (File.Exists(InputFile) && Path.GetExtension(InputFile).Equals(".vhdx", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhd.Disk).Assembly);
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhdx.Disk).Assembly);
                inputDisk = VirtualDisk.OpenDisk(InputFile, FileAccess.Read);
                stream = inputDisk.Content;
            }
            else if (File.Exists(InputFile))
            {
                stream = new FileStream(InputFile, FileMode.Open);
            }
            else
            {
                Logging.Log("Unknown input specified");
                return;
            }

            (FlashPart[] flashParts, ulong EndOfPLATPartition, List<GPT.Partition> partitions) = ImageSplitter.GetImageSlices(stream, BlockSize, ExcludedPartitionNames, SectorSize);

            IOrderedEnumerable<FlashingPayload> FlashingPayloadCollections = FlashingPayloadGenerator.GetOptimizedPayloads(flashParts, BlockSize, MaximumNumberOfBlankBlocksAllowed).OrderBy(x => x.WriteDescriptors.First());

            Logging.Log("");
            Logging.Log("Building image headers...");

            string FirstHeaderFilePath = Path.GetTempFileName();
            FileStream FirstHeaderFileStream = new(FirstHeaderFilePath, FileMode.OpenOrCreate);

            // ==============================
            // Header 1 start

            ImageHeader Image = new();
            FullFlash FullFlash = new();
            Store Store = new();

            // Todo make this read the image itself
            FullFlash.OSVersion = OperatingSystemVersion;
            FullFlash.DevicePlatformId0 = PlatformID;
            FullFlash.AntiTheftVersion = AntiTheftVersion;

            Store.SectorSize = SectorSize;
            Store.MinSectorCount = (UInt32)(stream.Length / Store.SectorSize);

            Logging.Log("Generating image manifest...");
            string manifest = ManifestIni.BuildUpManifest(FullFlash, Store, partitions);

            byte[] TextBytes = System.Text.Encoding.ASCII.GetBytes(manifest);

            Image.ManifestLength = (UInt32)TextBytes.Length;

            byte[] ImageHeaderBuffer = new byte[0x18];

            ByteOperations.WriteUInt32(ImageHeaderBuffer, 0, Image.Size);
            ByteOperations.WriteAsciiString(ImageHeaderBuffer, 0x04, Image.Signature);
            ByteOperations.WriteUInt32(ImageHeaderBuffer, 0x10, Image.ManifestLength);
            ByteOperations.WriteUInt32(ImageHeaderBuffer, 0x14, Image.ChunkSize);

            FirstHeaderFileStream.Write(ImageHeaderBuffer, 0, 0x18);
            FirstHeaderFileStream.Write(TextBytes, 0, TextBytes.Length);

            RoundUpToChunks(FirstHeaderFileStream, BlockSize);

            // Header 1 stop + round
            // ==============================

            string SecondHeaderFilePath = Path.GetTempFileName();
            FileStream SecondHeaderFileStream = new(SecondHeaderFilePath, FileMode.OpenOrCreate);

            // ==============================
            // Header 2 start

            byte[] WriteDescriptorBuffer = GetWriteDescriptorsBuffer(FlashingPayloadCollections);
            UInt32 FlashOnlyTableIndex = 0;

            bool reachedEnd = false;
            foreach (FlashingPayload payload in FlashingPayloadCollections)
            {
                foreach (WriteDescriptor writeDescriptor in payload.WriteDescriptors)
                {
                    foreach (DiskLocation diskLocation in writeDescriptor.DiskLocations)
                    {
                        if (diskLocation.BlockIndex > EndOfPLATPartition)
                        {
                            reachedEnd = true;
                            break;
                        }

                        FlashOnlyTableIndex += 1;
                    }

                    if (reachedEnd)
                    {
                        break;
                    }
                }

                if (reachedEnd)
                {
                    break;
                }
            }

            StoreHeader store = new()
            {
                WriteDescriptorCount = (UInt32)FlashingPayloadCollections.Count(),
                WriteDescriptorLength = (UInt32)WriteDescriptorBuffer.Length,
                FlashOnlyTableIndex = FlashOnlyTableIndex,
                PlatformIds = [PlatformID],
                BlockSizeInBytes = BlockSize
            };

            byte[] StoreHeaderBuffer = store.GetResultingBuffer(StoreHeaderVersion.V1, StoreHeaderUpdateType.Full, StoreHeaderCompressionAlgorithm.None);
            SecondHeaderFileStream.Write(StoreHeaderBuffer, 0, StoreHeaderBuffer.Length);
            SecondHeaderFileStream.Write(WriteDescriptorBuffer, 0, (Int32)store.WriteDescriptorLength);

            RoundUpToChunks(SecondHeaderFileStream, BlockSize);

            // Header 2 stop + round
            // ==============================

            SecurityHeader security = new();

            FirstHeaderFileStream.Seek(0, SeekOrigin.Begin);
            SecondHeaderFileStream.Seek(0, SeekOrigin.Begin);

            security.HashTableSize = 0x20 * (UInt32)((FirstHeaderFileStream.Length + SecondHeaderFileStream.Length) / BlockSize);

            foreach (FlashingPayload payload in FlashingPayloadCollections)
            {
                foreach (WriteDescriptor writeDescriptor in payload.WriteDescriptors)
                {
                    security.HashTableSize += 0x20 * writeDescriptor.BlockDataEntry.BlockCount; // One Hash per Block
                }
            }

            byte[] HashTable = new byte[security.HashTableSize];
            BinaryWriter binaryWriter = new(new MemoryStream(HashTable));
            for (int i = 0; i < FirstHeaderFileStream.Length / BlockSize; i++)
            {
                byte[] buffer = new byte[BlockSize];
                FirstHeaderFileStream.Read(buffer, 0, (Int32)BlockSize);
                byte[] hash = SHA256.HashData(buffer);
                binaryWriter.Write(hash, 0, hash.Length);
            }

            for (int i = 0; i < SecondHeaderFileStream.Length / BlockSize; i++)
            {
                byte[] buffer = new byte[BlockSize];
                SecondHeaderFileStream.Read(buffer, 0, (Int32)BlockSize);
                byte[] hash = SHA256.HashData(buffer);
                binaryWriter.Write(hash, 0, hash.Length);
            }

            foreach (FlashingPayload payload in FlashingPayloadCollections)
            {
                foreach (byte[] chunkHash in payload.ChunkHashes)
                {
                    binaryWriter.Write(chunkHash, 0, chunkHash.Length);
                }
            }

            binaryWriter.Close();

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

            FileStream FFUFileStream = new(FFUFile, FileMode.CreateNew);

            FFUFileStream.Write(SecurityHeaderBuffer, 0, 0x20);

            FFUFileStream.Write(catalog, 0, (Int32)security.CatalogSize);
            FFUFileStream.Write(HashTable, 0, (Int32)security.HashTableSize);

            RoundUpToChunks(FFUFileStream, BlockSize);

            FirstHeaderFileStream.Seek(0, SeekOrigin.Begin);
            SecondHeaderFileStream.Seek(0, SeekOrigin.Begin);

            byte[] buff = new byte[FirstHeaderFileStream.Length];
            FirstHeaderFileStream.Read(buff, 0, (Int32)FirstHeaderFileStream.Length);

            FirstHeaderFileStream.Close();
            File.Delete(FirstHeaderFilePath);

            FFUFileStream.Write(buff, 0, buff.Length);

            buff = new byte[SecondHeaderFileStream.Length];
            SecondHeaderFileStream.Read(buff, 0, (Int32)SecondHeaderFileStream.Length);

            SecondHeaderFileStream.Close();
            File.Delete(SecondHeaderFilePath);

            FFUFileStream.Write(buff, 0, buff.Length);

            Logging.Log("Writing payloads...");
            UInt64 counter = 0;

            DateTime startTime = DateTime.Now;

            foreach (FlashingPayload FlashingPayloadCollection in FlashingPayloadCollections)
            {
                for (int i = 0; i < FlashingPayloadCollection.StreamIndexes.Length; i++)
                {
                    UInt32 StreamIndex = FlashingPayloadCollection.StreamIndexes[i];
                    FlashPart flashPart = flashParts[StreamIndex];
                    Stream Stream = flashPart.Stream;
                    Stream.Seek(FlashingPayloadCollection.StreamLocations[i], SeekOrigin.Begin);

                    byte[] buffer = new byte[BlockSize];
                    Stream.Read(buffer, 0, (Int32)BlockSize);
                    FFUFileStream.Write(buffer, 0, (Int32)BlockSize);
                    counter++;

                    ulong totalBytes = (UInt64)FlashingPayloadCollections.Count() * BlockSize;
                    ulong bytesRead = counter * BlockSize;
                    ulong sourcePosition = counter * BlockSize;
                    ulong currentBlockIndex = FlashingPayloadCollection.WriteDescriptors[0].DiskLocations[i].BlockIndex * BlockSize;
                    bool displayRed = currentBlockIndex < EndOfPLATPartition;
                    ShowProgress(totalBytes, startTime, bytesRead, sourcePosition, displayRed);
                }
            }

            FFUFileStream.Close();
            inputDisk?.Dispose();
            Logging.Log("");
        }

        private static void RoundUpToChunks(FileStream stream, UInt32 chunkSize)
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
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / BytesRead * (totalBytes - BytesRead));

            double speed = Math.Round(SourcePosition / 1024L / 1024L / timeSoFar.TotalSeconds);

            Logging.Log(string.Format($"{GetDismLikeProgBar(int.Parse((BytesRead * 100 / totalBytes).ToString()))} {speed}MB/s {remaining.TotalHours}:{remaining.Minutes}:{remaining.Seconds}.{remaining.Milliseconds}"), returnline: false, severity: DisplayRed ? Logging.LoggingLevel.Warning : Logging.LoggingLevel.Information);
        }

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
    }
}