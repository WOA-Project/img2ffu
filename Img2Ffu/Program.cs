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
using Img2Ffu.Manifest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;

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

                    GenerateFFU(o.InputFile, o.FFUFile, o.PlatformID, o.SectorSize, o.BlockSize, o.AntiTheftVersion, o.OperatingSystemVersion, File.ReadAllLines(ExcludedPartitionNamesFilePath), o.MaximumNumberOfBlankBlocksAllowed);
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

        private static byte[] GetWriteDescriptorsBuffer(IEnumerable<BlockPayload> payloads, FlashUpdateVersion storeHeaderVersion)
        {
            using MemoryStream WriteDescriptorsStream = new();
            using BinaryWriter binaryWriter = new(WriteDescriptorsStream);

            foreach (BlockPayload payload in payloads)
            {
                byte[] WriteDescriptorBuffer = payload.WriteDescriptor.GetResultingBuffer(storeHeaderVersion);
                binaryWriter.Write(WriteDescriptorBuffer);
            }

            byte[] WriteDescriptorsBuffer = new byte[WriteDescriptorsStream.Length];
            WriteDescriptorsStream.Seek(0, SeekOrigin.Begin);
            WriteDescriptorsStream.ReadExactly(WriteDescriptorsBuffer, 0, WriteDescriptorsBuffer.Length);

            return WriteDescriptorsBuffer;
        }

        private static byte[] GenerateHashTable(MemoryStream FFUMetadataHeaderTempFileStream, IOrderedEnumerable<BlockPayload> BlockPayloads, uint BlockSize)
        {
            FFUMetadataHeaderTempFileStream.Seek(0, SeekOrigin.Begin);

            using MemoryStream HashTableStream = new MemoryStream();
            using BinaryWriter binaryWriter = new(HashTableStream);

            for (int i = 0; i < FFUMetadataHeaderTempFileStream.Length / BlockSize; i++)
            {
                byte[] buffer = new byte[BlockSize];
                FFUMetadataHeaderTempFileStream.Read(buffer, 0, (int)BlockSize);
                byte[] hash = SHA256.HashData(buffer);
                binaryWriter.Write(hash, 0, hash.Length);
            }

            foreach (BlockPayload payload in BlockPayloads)
            {
                binaryWriter.Write(payload.ChunkHash, 0, payload.ChunkHash.Length);
            }

            byte[] HashTableBuffer = new byte[HashTableStream.Length];
            HashTableStream.Seek(0, SeekOrigin.Begin);
            HashTableStream.ReadExactly(HashTableBuffer, 0, HashTableBuffer.Length);

            return HashTableBuffer;
        }

        /*
            *: Device Targeting Infos is optional
            **: Only available on V1_COMPRESSION FFU file formats
            ***: Only available on V2 FFU file formats
            
            - Validation Descriptor is always of size 0
            - While it is possible in the struct to specify more than one Block
              for a BlockDataEntry it shall only be equal to 0
            - The hash table contains every hash of every block in the FFU file
              starting from the Image Header to the end
            - When using V1_COMPRESSION FFU file format, BlockDataEntry contains
              an extra entry of size 4 bytes
            - Multiple locations for a block data entry only copies the block
              to multiple places
            
            +------------------------------+
            |                              |
            |       Security Header        |
            |                              |
            +------------------------------+
            |                              |
            |      Security Catalog        |
            |                              |
            +------------------------------+
            |                              |
            |         Hash Table           |
            |                              |
            +------------------------------+
            |                              |
            |     (Block Size) Padding     |
            |                              |
            +------------------------------+
            |                              |
            |         Image Header         |
            |                              |
            +------------------------------+
            |              *               |
            |    Image Header Extended     |
            |   DeviceTargetingInfoCount   |
            |                              |
            +------------------------------+
            |                              |
            |        Image Manifest        |
            |                              |
            +------------------------------+
            |              *               |
            |  DeviceTargetInfoLengths[0]  |
            |                              |
            +------------------------------+
            |              *               |
            |  DeviceTargetInfoStrings[0]  |
            |                              |
            +------------------------------+
            |              *               |
            |            . . .             |
            |                              |
            +------------------------------+
            |              *               |
            |  DeviceTargetInfoLengths[n]  |
            |                              |
            +------------------------------+
            |              *               |
            |  DeviceTargetInfoStrings[n]  |
            |                              |
            +------------------------------+
            |                              |
            |     (Block Size) Padding     |
            |                              |
            +------------------------------+
            |                              |
            |        Store Header[0]       |
            |                              |
            +------------------------------+
            |             * *              |
            |      CompressionAlgo[0]      |
            |                              |
            +------------------------------+
            |            * * *             |
            |      Store Header Ex[0]      |
            |                              |
            +------------------------------+
            |                              |
            |   Validation Descriptor[0]   |
            |                              |
            +------------------------------+
            |                              |
            |     Write Descriptors[0]     |
            |(BlockDataEntry+DiskLocations)|
            +------------------------------+
            |                              |
            |   (Block Size) Padding[0]    |
            |                              |
            +------------------------------+
            |            * * *             |
            |            . . .             |
            |                              |
            +------------------------------+
            |            * * *             |
            |        Store Header[n]       |
            |                              |
            +------------------------------+
            |            * * *             |
            |      Store Header Ex[n]      |
            |                              |
            +------------------------------+
            |            * * *             |
            |   Validation Descriptor[n]   |
            |                              |
            +------------------------------+
            |            * * *             |
            |     Write Descriptors[n]     |
            |(BlockDataEntry+DiskLocations)|
            +------------------------------+
            |            * * *             |
            |   (Block Size) Padding[n]    |
            |                              |
            +------------------------------+
            |                              |
            |         Data Blocks          |
            |                              |
            +------------------------------+
        */

        private static uint GetFlashOnlyTableIndex(IOrderedEnumerable<BlockPayload> BlockPayloads, ulong EndOfPLATPartition)
        {
            uint FlashOnlyTableIndex = 0;

            foreach (BlockPayload payload in BlockPayloads)
            {
                foreach (DiskLocation diskLocation in payload.WriteDescriptor.DiskLocations)
                {
                    if (diskLocation.BlockIndex > EndOfPLATPartition)
                    {
                        return FlashOnlyTableIndex;
                    }

                    FlashOnlyTableIndex += 1;
                }
            }

            return 0;
        }

        private static (uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, IOrderedEnumerable<BlockPayload> BlockPayloads, FlashPart[] flashParts, VirtualDisk InputDisk) GenerateStore(string InputFile, string[] PlatformIDs, uint SectorSize, uint BlockSize, string[] ExcludedPartitionNames, uint MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion FlashUpdateVersion)
        {
            Logging.Log("Opening input file...");

            Stream InputStream;
            VirtualDisk InputDisk = null;

            if (InputFile.Contains(@"\\.\physicaldrive", StringComparison.CurrentCultureIgnoreCase))
            {
                InputStream = new Streams.DeviceStream(InputFile, FileAccess.Read);
            }
            else if (File.Exists(InputFile) && Path.GetExtension(InputFile).Equals(".vhd", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhd.Disk).Assembly);
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhdx.Disk).Assembly);
                InputDisk = VirtualDisk.OpenDisk(InputFile, FileAccess.Read);
                InputStream = InputDisk.Content;
            }
            else if (File.Exists(InputFile) && Path.GetExtension(InputFile).Equals(".vhdx", StringComparison.InvariantCultureIgnoreCase))
            {
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhd.Disk).Assembly);
                DiscUtils.Setup.SetupHelper.RegisterAssembly(typeof(DiscUtils.Vhdx.Disk).Assembly);
                InputDisk = VirtualDisk.OpenDisk(InputFile, FileAccess.Read);
                InputStream = InputDisk.Content;
            }
            else if (File.Exists(InputFile))
            {
                InputStream = new FileStream(InputFile, FileMode.Open);
            }
            else
            {
                Logging.Log("Unknown input specified");
                return (0, null, null, null, null, null, null);
            }

            Logging.Log("Generating Image Slices...");
            (FlashPart[] flashParts, ulong EndOfPLATPartition, List<GPT.Partition> partitions) = ImageSplitter.GetImageSlices(InputStream, BlockSize, ExcludedPartitionNames, SectorSize);

            Logging.Log("Generating Block Payloads...");
            IOrderedEnumerable<BlockPayload> BlockPayloads = BlockPayloadsGenerator.GetOptimizedPayloads(flashParts, BlockSize, MaximumNumberOfBlankBlocksAllowed).OrderBy(x => x.WriteDescriptor.DiskLocations[0].BlockIndex);

            Logging.Log("Generating write descriptors...");
            byte[] WriteDescriptorBuffer = GetWriteDescriptorsBuffer(BlockPayloads, FlashUpdateVersion);

            Logging.Log("Generating store header...");
            StoreHeader store = new()
            {
                WriteDescriptorCount = (uint)BlockPayloads.Count(),
                WriteDescriptorLength = (uint)WriteDescriptorBuffer.Length,
                FlashOnlyTableIndex = GetFlashOnlyTableIndex(BlockPayloads, EndOfPLATPartition),
                BlockSize = BlockSize
                PlatformIds = PlatformIDs,
            };

            byte[] StoreHeaderBuffer = store.GetResultingBuffer(FlashUpdateVersion, FlashUpdateType.Full, CompressionAlgorithm.None);

            uint MinSectorCount = (uint)(InputStream.Length / SectorSize);

            return (MinSectorCount, partitions, StoreHeaderBuffer, WriteDescriptorBuffer, BlockPayloads, flashParts, InputDisk);
        }

        private static void GenerateFFU(string InputFile, string FFUFile, string PlatformID, uint SectorSize, uint BlockSize, string AntiTheftVersion, string OperatingSystemVersion, string[] ExcludedPartitionNames, uint MaximumNumberOfBlankBlocksAllowed)
        {
            if (File.Exists(FFUFile))
            {
                Logging.Log("File already exists!", Logging.LoggingLevel.Error);
                return;
            }

            FlashUpdateVersion FlashUpdateVersion = FlashUpdateVersion.V1;
            List<DeviceTargetInfo> deviceTargetInfos = [];
            string[] PlatformIDs = [PlatformID];

            Logging.Log($"Input image: {InputFile}");
            Logging.Log($"Destination image: {FFUFile}");
            Logging.Log($"Platform ID: {PlatformID}");
            Logging.Log($"Sector Size: {SectorSize}");
            Logging.Log($"Block Size: {BlockSize}");
            Logging.Log($"Anti Theft Version: {AntiTheftVersion}");
            Logging.Log($"OS Version: {OperatingSystemVersion}");
            Logging.Log("");

            (uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, IOrderedEnumerable<BlockPayload> BlockPayloads, FlashPart[] flashParts, VirtualDisk InputDisk) = GenerateStore(InputFile, PlatformIDs, SectorSize, BlockSize, ExcludedPartitionNames, MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion);

            // Todo make this read the image itself
            Logging.Log("Generating full flash manifest...");
            FullFlashManifest FullFlash = new()
            {
                OSVersion = OperatingSystemVersion,
                DevicePlatformId0 = PlatformID,
                AntiTheftVersion = AntiTheftVersion
            };

            Logging.Log("Generating store manifest...");
            StoreManifest Store = new()
            {
                SectorSize = SectorSize,
                MinSectorCount = MinSectorCount
            };

            Logging.Log("Generating image manifest...");
            string ImageManifest = ManifestIni.BuildUpManifest(FullFlash, Store, partitions);
            byte[] ManifestBuffer = System.Text.Encoding.ASCII.GetBytes(ImageManifest);


            Logging.Log("Generating image header...");
            ImageHeader ImageHeader = new()
            {
                ManifestLength = (uint)ManifestBuffer.Length
            };

            byte[] ImageHeaderBuffer = ImageHeader.GetResultingBuffer(BlockSize, deviceTargetInfos.Count != 0, (uint)deviceTargetInfos.Count);

            using MemoryStream FFUMetadataHeaderStream = new();

            //
            // Image Header
            //
            Logging.Log("Writing Image Header...");
            FFUMetadataHeaderStream.Write(ImageHeaderBuffer, 0, ImageHeaderBuffer.Length);

            //
            // Image Manifest
            //
            Logging.Log("Writing Image Manifest...");
            FFUMetadataHeaderStream.Write(ManifestBuffer, 0, ManifestBuffer.Length);

            if (deviceTargetInfos.Count != 0)
            {
                //
                // Device Target Infos...
                //
                Logging.Log("Writing Device Target Infos...");
                foreach (DeviceTargetInfo deviceTargetInfo in deviceTargetInfos)
                {
                    byte[] deviceTargetInfoBuffer = deviceTargetInfo.GetResultingBuffer();
                    FFUMetadataHeaderStream.Write(deviceTargetInfoBuffer, 0, deviceTargetInfoBuffer.Length);
                }
            }

            //
            // (Block Size) Padding
            //
            Logging.Log("Writing Padding...");
            RoundUpToChunks(FFUMetadataHeaderStream, BlockSize);

            //
            // Store Header[0]
            //
            Logging.Log("Writing Store Header...");
            FFUMetadataHeaderStream.Write(StoreHeaderBuffer, 0, StoreHeaderBuffer.Length);

            //
            // Write Descriptors[0]
            //
            Logging.Log("Writing Write Descriptors...");
            FFUMetadataHeaderStream.Write(WriteDescriptorBuffer, 0, WriteDescriptorBuffer.Length);

            //
            // (Block Size) Padding[0]
            //
            Logging.Log("Writing Padding...");
            RoundUpToChunks(FFUMetadataHeaderStream, BlockSize);

            Logging.Log("Generating image hash table...");
            byte[] HashTable = GenerateHashTable(FFUMetadataHeaderStream, BlockPayloads, BlockSize);

            Logging.Log("Generating image catalog...");
            byte[] CatalogBuffer = GenerateCatalogFile(HashTable);

            Logging.Log("Generating Security Header...");
            SecurityHeader security = new()
            {
                HashTableSize = (uint)HashTable.Length,
                CatalogSize = (uint)CatalogBuffer.Length
            };

            byte[] SecurityHeaderBuffer = security.GetResultingBuffer(BlockSize);

            byte[] FFUMetadataHeaderBuffer = new byte[FFUMetadataHeaderStream.Length];
            FFUMetadataHeaderStream.Seek(0, SeekOrigin.Begin);
            FFUMetadataHeaderStream.Read(FFUMetadataHeaderBuffer, 0, (int)FFUMetadataHeaderStream.Length);

            Logging.Log("Opening FFU file for writing...");
            WriteFFUFile(FFUFile, SecurityHeaderBuffer, CatalogBuffer, HashTable, FFUMetadataHeaderBuffer, BlockPayloads, flashParts, BlockSize);

            InputDisk?.Dispose();
        }

        private static void WriteFFUFile(string FFUFile, byte[] SecurityHeaderBuffer, byte[] CatalogBuffer, byte[] HashTable, byte[] FFUMetadataHeaderBuffer, IOrderedEnumerable<BlockPayload> BlockPayloads, FlashPart[] flashParts, uint BlockSize)
        {
            FileStream FFUFileStream = new(FFUFile, FileMode.CreateNew);

            //
            // Security Header
            //
            Logging.Log("Writing Security Header...");
            FFUFileStream.Write(SecurityHeaderBuffer, 0, SecurityHeaderBuffer.Length);

            //
            // Security Catalog
            //
            Logging.Log("Writing Security Catalog...");
            FFUFileStream.Write(CatalogBuffer, 0, CatalogBuffer.Length);

            //
            // Hash Table
            //
            Logging.Log("Writing Hash Table...");
            FFUFileStream.Write(HashTable, 0, HashTable.Length);

            //
            // (Block Size) Padding
            //
            Logging.Log("Writing Padding...");
            RoundUpToChunks(FFUFileStream, BlockSize);

            //
            // Image Header
            // Image Manifest
            // (Block Size) Padding
            // Store Header[0]
            // Write Descriptors[0]
            // (Block Size) Padding[0]
            //
            Logging.Log("Writing Image Header...");
            Logging.Log("Writing Image Manifest...");
            Logging.Log("Writing Padding...");
            Logging.Log("Writing Store Header...");
            Logging.Log("Writing Write Descriptors...");
            Logging.Log("Writing Padding...");
            FFUFileStream.Write(FFUMetadataHeaderBuffer, 0, FFUMetadataHeaderBuffer.Length);

            DateTime startTime = DateTime.Now;

            //
            // Data Blocks
            //
            Logging.Log("Writing Data Blocks...");
            for (ulong CurrentBlockIndex = 0; CurrentBlockIndex < (ulong)BlockPayloads.Count(); CurrentBlockIndex++)
            {
                BlockPayload BlockPayload = BlockPayloads.ElementAt((int)CurrentBlockIndex);
                uint FlashPartIndex = BlockPayload.FlashPartIndex;
                FlashPart FlashPart = flashParts[FlashPartIndex];
                Stream FlashPartStream = FlashPart.Stream;
                FlashPartStream.Seek(BlockPayload.FlashPartStreamLocation, SeekOrigin.Begin);

                byte[] BlockBuffer = new byte[BlockSize];
                FlashPartStream.Read(BlockBuffer, 0, (int)BlockSize);
                FFUFileStream.Write(BlockBuffer, 0, (int)BlockSize);

                ulong totalBytes = (ulong)BlockPayloads.Count() * BlockSize;
                ulong bytesRead = CurrentBlockIndex * BlockSize;
                ulong sourcePosition = CurrentBlockIndex * BlockSize;

                ShowProgress(totalBytes, startTime, bytesRead, sourcePosition);
            }

            FFUFileStream.Close();
            Logging.Log("");
        }

        private static void RoundUpToChunks(FileStream stream, uint chunkSize)
        {
            long Size = stream.Length;
            if ((Size % chunkSize) > 0)
            {
                long padding = (uint)(((Size / chunkSize) + 1) * chunkSize) - Size;
                stream.Write(new byte[padding], 0, (int)padding);
            }
        }

        private static void RoundUpToChunks(MemoryStream stream, uint chunkSize)
        {
            long Size = stream.Length;
            if ((Size % chunkSize) > 0)
            {
                long padding = (uint)(((Size / chunkSize) + 1) * chunkSize) - Size;
                stream.Write(new byte[padding], 0, (int)padding);
            }
        }

        private static void ShowProgress(ulong TotalBytes, DateTime startTime, ulong BytesRead, ulong SourcePosition)
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

            Logging.Log(string.Format($"{GetDismLikeProgBar(int.Parse((BytesRead * 100 / TotalBytes).ToString()))} {speed}MB/s {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}"), returnline: false, severity: Logging.LoggingLevel.Information);
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