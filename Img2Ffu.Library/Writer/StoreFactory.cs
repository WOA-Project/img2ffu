using DiscUtils;
using Img2Ffu.Writer.Data;
using Img2Ffu.Writer.Flashing;
using Img2Ffu.Writer;

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

        internal static (uint MinSectorCount, List<GPT.Partition> partitions, byte[] StoreHeaderBuffer, byte[] WriteDescriptorBuffer, KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads, VirtualDisk InputDisk) GenerateStore(string InputFile, string[] PlatformIDs, uint SectorSize, uint BlockSize, string[] ExcludedPartitionNames, uint MaximumNumberOfBlankBlocksAllowed, FlashUpdateVersion FlashUpdateVersion, ILogging Logging, bool IsFixedDiskLength = true)
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
                return (0, null, null, null, null, null);
            }

            Logging.Log("Generating Image Slices...");
            (FlashPart[] flashParts, List<GPT.Partition> partitions) = ImageSplitter.GetImageSlices(InputStream, BlockSize, ExcludedPartitionNames, SectorSize, Logging);

            Logging.Log("Generating Block Payloads...");
            KeyValuePair<ByteArrayKey, BlockPayload>[] BlockPayloads = BlockPayloadsGenerator.GetOptimizedPayloads(flashParts, BlockSize, MaximumNumberOfBlankBlocksAllowed, Logging);

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

            return (MinSectorCount, partitions, StoreHeaderBuffer, WriteDescriptorBuffer, BlockPayloads, InputDisk);
        }
    }
}
