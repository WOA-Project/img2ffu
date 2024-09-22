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
using DiscUtils;
using Img2Ffu.Writer.Data;
using Img2Ffu.Writer.Flashing;
using Img2Ffu.Writer.Manifest;
using System.Security.Cryptography;
using System.Text;

namespace Img2Ffu.Writer
{
    public static class FFUFactory
    {
        private static byte[] GenerateHashTable(
            MemoryStream FFUMetadataHeaderTempFileStream,
            IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads,
            uint BlockSize)
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

            _ = HashTableStream.Seek(0, SeekOrigin.Begin);

            byte[] HashTableBuffer = new byte[HashTableStream.Length];
            HashTableStream.ReadExactly(HashTableBuffer, 0, HashTableBuffer.Length);

            return HashTableBuffer;
        }

        public static void GenerateFFU(
            IEnumerable<InputForStore> InputsForStores,
            string FFUFile,
            IEnumerable<string> PlatformIDs,
            uint SectorSize,
            uint BlockSize,
            string AntiTheftVersion,
            string OperatingSystemVersion,
            FlashUpdateVersion FlashUpdateVersion,
            List<DeviceTargetInfo> deviceTargetingInformationArray,
            ILogging Logging)
        {
            if (File.Exists(FFUFile))
            {
                Logging.Log("File already exists!", ILoggingLevel.Error);
                return;
            }

            if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(FFUFile))))
            {
                Logging.Log("Directory to place the FFU file into does not exist!", ILoggingLevel.Error);
                return;
            }

            if (!InputsForStores.Any())
            {
                Logging.Log("At least one store must be specified in order to generate a FFU file!", ILoggingLevel.Error);
                return;
            }

            if (InputsForStores.Count() > 1 && FlashUpdateVersion != FlashUpdateVersion.V2)
            {
                Logging.Log("Multiple stores is only supported with Flash Update Version 2!", ILoggingLevel.Error);
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
                DevicePlatformId3 = PlatformIDs.Count() > 3 ? PlatformIDs.ElementAt(3) : "",
                DevicePlatformId2 = PlatformIDs.Count() > 2 ? PlatformIDs.ElementAt(2) : "",
                DevicePlatformId1 = PlatformIDs.Count() > 1 ? PlatformIDs.ElementAt(1) : "",
                DevicePlatformId0 = PlatformIDs.ElementAt(0),
                AntiTheftVersion = AntiTheftVersion
            };

            List<(
                uint MinSectorCount, 
                List<GPT.Partition> partitions, 
                byte[] StoreHeaderBuffer, 
                byte[] WriteDescriptorBuffer, 
                KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, 
                VirtualDisk? InputDisk
            )> StoreGenerationParameters = [];

            ushort StoreIndex = 0;
            ushort StoreCount = (ushort)InputsForStores.Count();

            foreach (InputForStore inputForStore in InputsForStores)
            {
                // FFU Stores indexing starts from 1, not 0
                StoreIndex++;

                Logging.Log($"[Store #{StoreIndex}] Input image: {inputForStore.InputFile}");
                Logging.Log($"[Store #{StoreIndex}] Device Path: {inputForStore.DevicePath}");
                Logging.Log($"[Store #{StoreIndex}] Is Fixed Disk Length: {inputForStore.IsFixedDiskLength}");

                (
                    uint MinSectorCount, 
                    List<GPT.Partition> partitions, 
                    byte[] StoreHeaderBuffer, 
                    byte[] WriteDescriptorBuffer, 
                    KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, 
                    VirtualDisk? InputDisk
                ) GeneratedStoreParameters = StoreFactory.GenerateStore(
                    inputForStore,
                    PlatformIDs,
                    SectorSize,
                    BlockSize,
                    FlashUpdateVersion,
                    Logging,
                    StoreCount,
                    StoreIndex);

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

            byte[] ImageHeaderBuffer = ImageHeader.GetResultingBuffer(BlockSize, deviceTargetingInformationArray.Count != 0, (uint)deviceTargetingInformationArray.Count);

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

            if (deviceTargetingInformationArray.Count != 0)
            {
                //
                // Device Target Information...
                //
                Logging.Log("Writing Device Target Information Array...");
                foreach (DeviceTargetInfo deviceTargetInfo in deviceTargetingInformationArray)
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

            foreach ((
                uint _, 
                List<GPT.Partition> _, 
                byte[] StoreHeaderBuffer, 
                byte[] WriteDescriptorBuffer, 
                KeyValuePair<ByteArrayKey, BlockPayload>[] _, 
                VirtualDisk? _
            ) in StoreGenerationParameters)
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

            _ = FFUMetadataHeaderStream.Seek(0, SeekOrigin.Begin);

            byte[] FFUMetadataHeaderBuffer = new byte[FFUMetadataHeaderStream.Length];
            _ = FFUMetadataHeaderStream.Read(FFUMetadataHeaderBuffer, 0, (int)FFUMetadataHeaderStream.Length);

            Logging.Log("Opening FFU file for writing...");
            WriteFFUFile(FFUFile, SecurityHeaderBuffer, CatalogBuffer, HashTable, FFUMetadataHeaderBuffer, BlockPayloads, BlockSize, Logging);

            Logging.Log("Disposing Resources...");
            StoreGenerationParameters.ForEach(x => x.InputDisk?.Dispose());
        }

        private static void WriteFFUFile(
            string FFUFile,
            byte[] SecurityHeaderBuffer,
            byte[] CatalogBuffer,
            byte[] HashTable,
            byte[] FFUMetadataHeaderBuffer,
            IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads,
            uint BlockSize,
            ILogging Logging)
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
            Logging.Log("Writing FFU Metadata Header...");
            FFUFileStream.Write(FFUMetadataHeaderBuffer, 0, FFUMetadataHeaderBuffer.Length);

            //
            // Data Blocks
            //
            Logging.Log("Writing Data Blocks...");

            DateTime startTime = DateTime.Now;
            ulong totalBytes = (ulong)BlockPayloads.LongCount() * BlockSize;

            for (ulong CurrentBlockIndex = 0; CurrentBlockIndex < (ulong)BlockPayloads.LongCount(); CurrentBlockIndex++)
            {
                BlockPayload BlockPayload = BlockPayloads.ElementAt((int)CurrentBlockIndex).Value;
                byte[] BlockBuffer = BlockPayload.ReadBlock(BlockSize);

                FFUFileStream.Write(BlockBuffer, 0, (int)BlockSize);

                ulong bytesRead = CurrentBlockIndex * BlockSize;

                LoggingHelpers.ShowProgress(totalBytes, bytesRead, bytesRead, startTime, Logging);
            }
            Logging.Log("");

            FFUFileStream.Close();
        }
    }
}
