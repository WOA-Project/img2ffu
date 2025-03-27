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
        private static Span<byte> GenerateHashTable(
            MemoryStream FFUMetadataHeaderTempFileStream,
            IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads,
            uint BlockSize)
        {
            _ = FFUMetadataHeaderTempFileStream.Seek(0, SeekOrigin.Begin);

            using MemoryStream HashTableStream = new();
            using BinaryWriter binaryWriter = new(HashTableStream);

            Memory<byte> buffer = new byte[BlockSize];
            Span<byte> bufferSpan = buffer.Span;

            for (int i = 0; i < FFUMetadataHeaderTempFileStream.Length / BlockSize; i++)
            {
                _ = FFUMetadataHeaderTempFileStream.Read(bufferSpan);

                Span<byte> hash = SHA256.HashData(bufferSpan);
                binaryWriter.Write(hash);
            }

            foreach (KeyValuePair<ByteArrayKey, BlockPayload> payload in BlockPayloads)
            {
                binaryWriter.Write(payload.Key.Bytes);
            }

            _ = HashTableStream.Seek(0, SeekOrigin.Begin);

            Memory<byte> HashTableBuffer = new byte[HashTableStream.Length];
            Span<byte> span = HashTableBuffer.Span;
            HashTableStream.ReadExactly(span);

            return span;
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
            string SecureBootSigningCommand,
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
                Memory<byte> StoreHeaderBuffer,
                Memory<byte> WriteDescriptorBuffer,
                List<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads,
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
                    Memory<byte> StoreHeaderBuffer,
                    Memory<byte> WriteDescriptorBuffer,
                    List<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads,
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
            Span<byte> ManifestBuffer = Encoding.ASCII.GetBytes(ImageManifest);

            Logging.Log("Generating image header...");
            ImageHeader ImageHeader = new()
            {
                ManifestLength = (uint)ManifestBuffer.Length
            };

            Span<byte> ImageHeaderBuffer = ImageHeader.GetResultingBuffer(BlockSize, deviceTargetingInformationArray.Count != 0, (uint)deviceTargetingInformationArray.Count);

            using MemoryStream FFUMetadataHeaderStream = new();

            //
            // Image Header
            //
            Logging.Log("Writing Image Header...");
            FFUMetadataHeaderStream.Write(ImageHeaderBuffer);

            //
            // Image Manifest
            //
            Logging.Log("Writing Image Manifest...");
            FFUMetadataHeaderStream.Write(ManifestBuffer);

            if (deviceTargetingInformationArray.Count != 0)
            {
                //
                // Device Target Information...
                //
                Logging.Log("Writing Device Target Information Array...");
                foreach (DeviceTargetInfo deviceTargetInfo in deviceTargetingInformationArray)
                {
                    Span<byte> deviceTargetInfoBuffer = deviceTargetInfo.GetResultingBuffer();
                    FFUMetadataHeaderStream.Write(deviceTargetInfoBuffer);
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
                Memory<byte> StoreHeaderBuffer,
                Memory<byte> WriteDescriptorBuffer,
                List<KeyValuePair<ByteArrayKey, BlockPayload>> _,
                VirtualDisk? _
            ) in StoreGenerationParameters)
            {
                //
                // Store Header[0]
                //
                Logging.Log("Writing Store Header...");
                FFUMetadataHeaderStream.Write(StoreHeaderBuffer.Span);

                //
                // Write Descriptors[0]
                //
                Logging.Log("Writing Write Descriptors...");
                FFUMetadataHeaderStream.Write(WriteDescriptorBuffer.Span);

                //
                // (Block Size) Padding[0]
                //
                Logging.Log("Writing Padding...");
                ChunkUtils.RoundUpToChunks(FFUMetadataHeaderStream, BlockSize);
            }

            Logging.Log("Generating image hash table...");
            Span<byte> HashTable = GenerateHashTable(FFUMetadataHeaderStream, BlockPayloads, BlockSize);

            Logging.Log("Generating image catalog...");
            byte[] CatalogBuffer = CatalogFactory.GenerateCatalogFile(HashTable);

            if (!string.IsNullOrEmpty(SecureBootSigningCommand))
            {
                Logging.Log("Signing image catalog...");
                CatalogBuffer = CatalogFactory.SignCatalogFile(CatalogBuffer, SecureBootSigningCommand);
            }

            Logging.Log("Generating Security Header...");
            SecurityHeader security = new()
            {
                HashTableSize = (uint)HashTable.Length,
                CatalogSize = (uint)CatalogBuffer.Length
            };

            Span<byte> SecurityHeaderBuffer = security.GetResultingBuffer(BlockSize);

            _ = FFUMetadataHeaderStream.Seek(0, SeekOrigin.Begin);

            Memory<byte> FFUMetadataHeaderBuffer = new byte[FFUMetadataHeaderStream.Length];
            Span<byte> FFUMetadataHeaderSpan = FFUMetadataHeaderBuffer.Span;
            _ = FFUMetadataHeaderStream.Read(FFUMetadataHeaderSpan);

            Logging.Log("Opening FFU file for writing...");
            WriteFFUFile(FFUFile, SecurityHeaderBuffer, CatalogBuffer, HashTable, FFUMetadataHeaderSpan, BlockPayloads, BlockSize, Logging);

            Logging.Log("Disposing Resources...");
            StoreGenerationParameters.ForEach(x => x.InputDisk?.Dispose());
        }

        private static void WriteFFUFile(
            string FFUFile,
            Span<byte> SecurityHeaderBuffer,
            Span<byte> CatalogBuffer,
            Span<byte> HashTable,
            Span<byte> FFUMetadataHeaderBuffer,
            IEnumerable<KeyValuePair<ByteArrayKey, BlockPayload>> BlockPayloads,
            uint BlockSize,
            ILogging Logging)
        {
            FileStream FFUFileStream = new(FFUFile, FileMode.CreateNew);

            //
            // Security Header
            //
            Logging.Log("Writing Security Header...");
            FFUFileStream.Write(SecurityHeaderBuffer);

            //
            // Security Catalog
            //
            Logging.Log("Writing Security Catalog...");
            FFUFileStream.Write(CatalogBuffer);

            //
            // Hash Table
            //
            Logging.Log("Writing Hash Table...");
            FFUFileStream.Write(HashTable);

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
            FFUFileStream.Write(FFUMetadataHeaderBuffer);

            //
            // Data Blocks
            //
            Logging.Log("Writing Data Blocks...");

            DateTime startTime = DateTime.Now;
            ulong totalBytes = (ulong)BlockPayloads.LongCount() * BlockSize;

            Memory<byte> BlockBuffer = new byte[BlockSize];
            Span<byte> span = BlockBuffer.Span;

            ulong CurrentBlockIndex = 0;
            foreach (KeyValuePair<ByteArrayKey, BlockPayload> BlockPayload in BlockPayloads)
            {
                BlockPayload.Value.ReadBlock(span);
                FFUFileStream.Write(span);

                CurrentBlockIndex++;
                ulong bytesRead = CurrentBlockIndex * BlockSize;

                LoggingHelpers.ShowProgress(totalBytes, bytesRead, bytesRead, startTime, Logging);
            }
            Logging.Log("");

            FFUFileStream.Close();
        }
    }
}
