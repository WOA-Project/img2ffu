using DiscUtils;
using Img2Ffu.Writer.Data;
using Img2Ffu.Writer.Flashing;
using Img2Ffu.Writer.Manifest;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Img2Ffu.Writer
{
    public class FFUFactory
    {

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

        private static byte[] GenerateCatalogFile2(byte[] hashData)
        {
            string catalog = Path.GetTempFileName();
            string cdf = Path.GetTempFileName();
            string hashTableBlob = Path.GetTempFileName();

            File.WriteAllBytes(hashTableBlob, hashData);

            using (StreamWriter streamWriter = new(cdf))
            {
                streamWriter.WriteLine("[CatalogHeader]");
                streamWriter.WriteLine("Name={0}", catalog);
                streamWriter.WriteLine("[CatalogFiles]");
                streamWriter.WriteLine("{0}={1}", "HashTable.blob", hashTableBlob);
            }

            using (Process process = new())
            {
                process.StartInfo.FileName = "MakeCat.exe";
                process.StartInfo.Arguments = string.Format("\"{0}\"", cdf);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;

                _ = process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception();
                }
            }

            byte[] catalogBuffer = File.ReadAllBytes(catalog);

            File.Delete(catalog);
            File.Delete(hashTableBlob);
            File.Delete(cdf);

            return catalogBuffer;
        }

        private static byte[] GetWriteDescriptorsBuffer(KeyValuePair<ByteArrayKey, BlockPayload>[] payloads, FlashUpdateVersion storeHeaderVersion)
        {
            using MemoryStream WriteDescriptorsStream = new();
            using BinaryWriter binaryWriter = new(WriteDescriptorsStream);

            foreach (KeyValuePair<ByteArrayKey, BlockPayload> payload in payloads)
            {
                byte[] WriteDescriptorBuffer = payload.Value.WriteDescriptor.GetResultingBuffer(storeHeaderVersion);
                binaryWriter.Write(WriteDescriptorBuffer);
            }

            byte[] WriteDescriptorsBuffer = new byte[WriteDescriptorsStream.Length];
            _ = WriteDescriptorsStream.Seek(0, SeekOrigin.Begin);
            WriteDescriptorsStream.ReadExactly(WriteDescriptorsBuffer, 0, WriteDescriptorsBuffer.Length);

            return WriteDescriptorsBuffer;
        }

        private static byte[] GenerateHashTable(MemoryStream FFUMetadataHeaderTempFileStream, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, uint BlockSize)
        {
            _ = FFUMetadataHeaderTempFileStream.Seek(0, SeekOrigin.Begin);

            using MemoryStream HashTableStream = new();
            using BinaryWriter binaryWriter = new(HashTableStream);

            for (int i = 0; i < FFUMetadataHeaderTempFileStream.Length / BlockSize; i++)
            {
                byte[] buffer = new byte[BlockSize];
                _ = FFUMetadataHeaderTempFileStream.Read(buffer, 0, (int)BlockSize);
                byte[] hash = SHA256.HashData(buffer);
                binaryWriter.Write(hash, 0, hash.Length);
            }

            foreach (KeyValuePair<ByteArrayKey, BlockPayload> payload in BlockPayloads)
            {
                binaryWriter.Write(payload.Key.Bytes, 0, payload.Key.Bytes.Length);
            }

            byte[] HashTableBuffer = new byte[HashTableStream.Length];
            _ = HashTableStream.Seek(0, SeekOrigin.Begin);
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

        private static (uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, FlashPart[] flashParts, VirtualDisk InputDisk) GenerateStore(string InputFile, string[] PlatformIDs, uint SectorSize, uint BlockSize, string[] ExcludedPartitionNames, uint MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion FlashUpdateVersion, ILogging Logging)
        {
            Logging.Log("Opening input file...");

            Stream InputStream;
            VirtualDisk? InputDisk = null;

            if (InputFile.Contains(@"\\.\physicaldrive", StringComparison.CurrentCultureIgnoreCase))
            {
                InputStream = new Img2Ffu.Streams.DeviceStream(InputFile, FileAccess.Read);
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
            (FlashPart[] flashParts, List<GPT.Partition> partitions) = ImageSplitter.GetImageSlices(InputStream, BlockSize, ExcludedPartitionNames, SectorSize, Logging);

            Logging.Log("Generating Block Payloads...");
            KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads = BlockPayloadsGenerator.GetOptimizedPayloads(flashParts, BlockSize, MaximumNumberOfBlankBlocksAllowed, Logging);

            bool IsFixedDiskLength = false; // POC!
            BlockPayloads = BlockPayloadsGenerator.GetGPTPayloads(BlockPayloads, InputStream, BlockSize, IsFixedDiskLength);

            Logging.Log("Generating write descriptors...");
            byte[] WriteDescriptorBuffer = GetWriteDescriptorsBuffer(BlockPayloads, FlashUpdateVersion);

            Logging.Log("Generating store header...");
            StoreHeader store = new()
            {
                WriteDescriptorCount = (uint)BlockPayloads.LongLength,
                WriteDescriptorLength = (uint)WriteDescriptorBuffer.Length,
                PlatformIds = PlatformIDs,
                BlockSize = BlockSize,

                // POC Begins
                NumberOfStores = 1,
                StoreIndex = 1,
                DevicePath = "VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A)",
                StorePayloadSize = (ulong)BlockPayloads.LongLength * BlockSize
                // POC Ends
            };

            byte[] StoreHeaderBuffer = store.GetResultingBuffer(FlashUpdateVersion, FlashUpdateType.Full, CompressionAlgorithm.None);

            uint MinSectorCount = (uint)(InputStream.Length / SectorSize);

            return (MinSectorCount, partitions, StoreHeaderBuffer, WriteDescriptorBuffer, BlockPayloads, flashParts, InputDisk);
        }

        public static void GenerateFFU(string InputFile, string FFUFile, string[] PlatformIDs, uint SectorSize, uint BlockSize, string AntiTheftVersion, string OperatingSystemVersion, string[] ExcludedPartitionNames, uint MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion FlashUpdateVersion, List<DeviceTargetInfo> deviceTargetInfos, ILogging Logging)
        {
            if (File.Exists(FFUFile))
            {
                Logging.Log("File already exists!", ILoggingLevel.Error);
                return;
            }

            Logging.Log($"Input image: {InputFile}");
            Logging.Log($"Destination image: {FFUFile}");
            Logging.Log($"Platform IDs: {string.Join("\nPlatform IDs: ", PlatformIDs)}");
            Logging.Log($"Sector Size: {SectorSize}");
            Logging.Log($"Block Size: {BlockSize}");
            Logging.Log($"Anti Theft Version: {AntiTheftVersion}");
            Logging.Log($"OS Version: {OperatingSystemVersion}");
            Logging.Log("");

            (uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, FlashPart[] flashParts, VirtualDisk InputDisk) = GenerateStore(InputFile, PlatformIDs, SectorSize, BlockSize, ExcludedPartitionNames, MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion, Logging);

            // Todo make this read the image itself
            Logging.Log("Generating full flash manifest...");
            FullFlashManifest FullFlash = new()
            {
                OSVersion = OperatingSystemVersion,
                DevicePlatformId3 = PlatformIDs.Count() > 3 ? PlatformIDs[3] : "",
                DevicePlatformId2 = PlatformIDs.Count() > 2 ? PlatformIDs[2] : "",
                DevicePlatformId1 = PlatformIDs.Count() > 1 ? PlatformIDs[1] : "",
                DevicePlatformId0 = PlatformIDs[0],
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
            byte[] ManifestBuffer = Encoding.ASCII.GetBytes(ImageManifest);


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
            _ = FFUMetadataHeaderStream.Seek(0, SeekOrigin.Begin);
            _ = FFUMetadataHeaderStream.Read(FFUMetadataHeaderBuffer, 0, (int)FFUMetadataHeaderStream.Length);

            Logging.Log("Opening FFU file for writing...");
            WriteFFUFile(FFUFile, SecurityHeaderBuffer, CatalogBuffer, HashTable, FFUMetadataHeaderBuffer, BlockPayloads, flashParts, BlockSize, Logging);

            InputDisk?.Dispose();
        }

        private static void WriteFFUFile(string FFUFile, byte[] SecurityHeaderBuffer, byte[] CatalogBuffer, byte[] HashTable, byte[] FFUMetadataHeaderBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, FlashPart[] flashParts, uint BlockSize, ILogging Logging)
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
            for (ulong CurrentBlockIndex = 0; CurrentBlockIndex < (ulong)BlockPayloads.LongLength; CurrentBlockIndex++)
            {
                BlockPayload BlockPayload = BlockPayloads.ElementAt((int)CurrentBlockIndex).Value;
                byte[] BlockBuffer = BlockPayload.ReadBlock(BlockSize);

                FFUFileStream.Write(BlockBuffer, 0, (int)BlockSize);

                ulong totalBytes = (ulong)BlockPayloads.LongLength * BlockSize;
                ulong bytesRead = CurrentBlockIndex * BlockSize;
                ulong sourcePosition = CurrentBlockIndex * BlockSize;

                ShowProgress(totalBytes, startTime, bytesRead, sourcePosition, Logging);
            }
            Logging.Log("");

            FFUFileStream.Close();
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

        private static void ShowProgress(ulong TotalBytes, DateTime startTime, ulong BytesRead, ulong SourcePosition, ILogging Logging)
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

            Logging.Log(string.Format($"{LoggingHelpers.GetDismLikeProgBar(int.Parse((BytesRead * 100 / TotalBytes).ToString()))} {speed}MB/s {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}"), returnLine: false, severity: ILoggingLevel.Information);
        }
    }
}
