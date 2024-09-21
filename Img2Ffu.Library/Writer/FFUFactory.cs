using DiscUtils;
using Img2Ffu.Writer.Data;
using Img2Ffu.Writer.Flashing;
using Img2Ffu.Writer.Manifest;
using System.Security.Cryptography;
using System.Text;

namespace Img2Ffu.Writer
{
    public class FFUFactory
    {
        private static byte[] GenerateHashTable(MemoryStream FFUMetadataHeaderTempFileStream, IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads, uint BlockSize)
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

        public static void GenerateFFU((string InputFile, string DevicePath, bool IsFixedDiskLength)[] InputsForStores, string FFUFile, string[] PlatformIDs, uint SectorSize, uint BlockSize, string AntiTheftVersion, string OperatingSystemVersion, string[] ExcludedPartitionNames, uint MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion FlashUpdateVersion, List<DeviceTargetInfo> deviceTargetInfos, ILogging Logging)
        {
            if (File.Exists(FFUFile))
            {
                Logging.Log("File already exists!", ILoggingLevel.Error);
                return;
            }

            Logging.Log($"Destination image: {FFUFile}");
            Logging.Log($"Platform IDs: {string.Join("\nPlatform IDs: ", PlatformIDs)}");
            Logging.Log($"Sector Size: {SectorSize}");
            Logging.Log($"Block Size: {BlockSize}");
            Logging.Log($"Anti Theft Version: {AntiTheftVersion}");
            Logging.Log($"OS Version: {OperatingSystemVersion}");
            Logging.Log("");

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

            List<(uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, VirtualDisk InputDisk)> StoreGenerationParameters = [];

            ushort StoreIndex = 0;

            foreach ((string InputFile, string DevicePath, bool IsFixedDiskLength) in InputsForStores)
            {
                // FFU Stores index starting from 1, not 0
                StoreIndex++;

                Logging.Log($"Input image: {InputFile}");
                Logging.Log($"Device Path: {DevicePath}");
                Logging.Log($"Is Fixed Disk Length: {IsFixedDiskLength}");

                (uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, VirtualDisk InputDisk) GeneratedStoreParameters = StoreFactory.GenerateStore(
                    InputFile,
                    PlatformIDs,
                    SectorSize,
                    BlockSize,
                    ExcludedPartitionNames,
                    MaximumNumberOfBlankBlocksAllowed,
                    FlashUpdateVersion,
                    Logging,
                    IsFixedDiskLength,
                    (ushort)InputsForStores.Length,
                    StoreIndex,
                    DevicePath);

                StoreGenerationParameters.Add(GeneratedStoreParameters);
            }

            IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads = StoreGenerationParameters.SelectMany(x => x.BlockPayloads);

            Logging.Log("Generating store manifest...");
            IEnumerable<(StoreManifest StoreManifest, List<GPT.Partition> partitions)> Stores = StoreGenerationParameters.Select(x =>
            {
                StoreManifest StoreManifest = new()
                {
                    SectorSize = SectorSize,
                    MinSectorCount = x.MinSectorCount
                };

                return (StoreManifest, x.partitions);
            });

            Logging.Log("Generating image manifest...");
            string ImageManifest = ManifestIni.BuildUpManifest(FullFlash, Stores);
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
            ChunkUtils.RoundUpToChunks(FFUMetadataHeaderStream, BlockSize);

            foreach ((uint _, List<GPT.Partition> _, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] _, VirtualDisk _) in StoreGenerationParameters)
            {
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
                ChunkUtils.RoundUpToChunks(FFUMetadataHeaderStream, BlockSize);
            }

            Logging.Log("Generating image hash table...");
            byte[] HashTable = GenerateHashTable(FFUMetadataHeaderStream, BlockPayloads, BlockSize);

            Logging.Log("Generating image catalog...");
            byte[] CatalogBuffer = CatalogFactory.GenerateCatalogFile(HashTable);

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
            WriteFFUFile(FFUFile, SecurityHeaderBuffer, CatalogBuffer, HashTable, FFUMetadataHeaderBuffer, BlockPayloads, BlockSize, Logging);

            Logging.Log("Disposing Resources...");
            StoreGenerationParameters.ForEach(x => x.InputDisk?.Dispose());
        }

        private static void WriteFFUFile(string FFUFile, byte[] SecurityHeaderBuffer, byte[] CatalogBuffer, byte[] HashTable, byte[] FFUMetadataHeaderBuffer, IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads, uint BlockSize, ILogging Logging)
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
            ChunkUtils.RoundUpToChunks(FFUFileStream, BlockSize);

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
            for (ulong CurrentBlockIndex = 0; CurrentBlockIndex < (ulong)BlockPayloads.LongCount(); CurrentBlockIndex++)
            {
                BlockPayload BlockPayload = BlockPayloads.ElementAt((int)CurrentBlockIndex).Value;
                byte[] BlockBuffer = BlockPayload.ReadBlock(BlockSize);

                FFUFileStream.Write(BlockBuffer, 0, (int)BlockSize);

                ulong totalBytes = (ulong)BlockPayloads.LongCount() * BlockSize;
                ulong bytesRead = CurrentBlockIndex * BlockSize;
                ulong sourcePosition = CurrentBlockIndex * BlockSize;

                LoggingHelpers.ShowProgress(totalBytes, bytesRead, sourcePosition, startTime, Logging);
            }
            Logging.Log("");

            FFUFileStream.Close();
        }
    }
}
