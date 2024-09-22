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
using Img2Ffu.Streams;
using Img2Ffu.Writer.Data;
using Img2Ffu.Writer.Flashing;

namespace Img2Ffu.Writer
{
    internal static class StoreFactory
    {
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

        private static (Stream InputStream, VirtualDisk? InputDisk) OpenInput(string InputFile, ILogging Logging)
        {
            Stream InputStream;
            VirtualDisk? InputDisk = null;

            if (InputFile.Contains(@"\\.\physicaldrive", StringComparison.CurrentCultureIgnoreCase))
            {
                InputStream = new DeviceStream(InputFile, FileAccess.Read);
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
                throw new Exception($"Unknown Input Specified: {InputFile}");
            }

            return (InputStream, InputDisk);
        }

        internal static (
            uint MinSectorCount, 
            List<GPT.Partition> partitions, 
            byte[] StoreHeaderBuffer, 
            byte[] WriteDescriptorBuffer, 
            KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, 
            VirtualDisk? InputDisk
        ) GenerateStore(
            InputForStore InputForStore,
            IEnumerable<string> PlatformIDs,
            uint SectorSize,
            uint BlockSize,
            FlashUpdateVersion FlashUpdateVersion,
            ILogging Logging,
            ushort NumberOfStores,
            ushort StoreIndex)
        {
            Logging.Log("Opening input file...");
            (Stream InputStream, VirtualDisk? InputDisk) = OpenInput(InputForStore.InputFile, Logging);

            Logging.Log("Generating Image Slices...");
            (FlashPart[] flashParts, List<GPT.Partition> partitions) = ImageSplitter.GetImageSlices(InputStream,
                                                                                                    BlockSize,
                                                                                                    InputForStore.ExcludedPartitionNames,
                                                                                                    SectorSize,
                                                                                                    Logging);

            Logging.Log("Generating Block Payloads...");
            KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads = BlockPayloadsGenerator.GetOptimizedPayloads(flashParts,
                                                                                                                   BlockSize,
                                                                                                                   InputForStore.MaximumNumberOfBlankBlocksAllowed,
                                                                                                                   Logging);

            BlockPayloads = BlockPayloadsGenerator.GetGPTPayloads(BlockPayloads, InputStream, BlockSize, InputForStore.IsFixedDiskLength);

            Logging.Log("Generating write descriptors...");
            byte[] WriteDescriptorBuffer = GetWriteDescriptorsBuffer(BlockPayloads, FlashUpdateVersion);

            Logging.Log("Generating store header...");
            StoreHeader store = new()
            {
                WriteDescriptorCount = (uint)BlockPayloads.LongLength,
                WriteDescriptorLength = (uint)WriteDescriptorBuffer.Length,
                PlatformIds = PlatformIDs.ToArray(),
                BlockSize = BlockSize,
                NumberOfStores = NumberOfStores,
                StoreIndex = StoreIndex,
                DevicePath = InputForStore.DevicePath,
                StorePayloadSize = (ulong)BlockPayloads.LongLength * BlockSize
            };

            byte[] StoreHeaderBuffer = store.GetResultingBuffer(FlashUpdateVersion, FlashUpdateType.Full, CompressionAlgorithm.None);

            uint MinSectorCount = (uint)(InputStream.Length / SectorSize);

            return (MinSectorCount, partitions, StoreHeaderBuffer, WriteDescriptorBuffer, BlockPayloads, InputDisk);
        }
    }
}
