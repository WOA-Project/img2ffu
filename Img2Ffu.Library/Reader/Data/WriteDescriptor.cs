using Img2Ffu.Reader.Enums;
using Img2Ffu.Reader.Structs;
using System.Text;

namespace Img2Ffu.Reader.Data
{
    internal class WriteDescriptor
    {
        public BlockDataEntry BlockDataEntry;
        public uint DataSize;

        public readonly List<DiskLocation> DiskLocations = [];
        private readonly bool HasDataSizeField;

        public WriteDescriptor(Stream stream, FFUVersion ffuVersion)
        {
            HasDataSizeField = ffuVersion == FFUVersion.V1_COMPRESSED;

            BlockDataEntry = stream.ReadStructure<BlockDataEntry>();

            if (HasDataSizeField)
            {
                using BinaryReader binaryReader = new(stream, Encoding.ASCII, true);
                DataSize = binaryReader.ReadUInt32();
            }

            for (uint i = 0; i < BlockDataEntry.LocationCount; i++)
            {
                DiskLocations.Add(stream.ReadStructure<DiskLocation>());
            }
        }

        public WriteDescriptor(BlockDataEntry blockDataEntry, List<DiskLocation> diskLocations, uint compressedDataBlockSize)
        {
            HasDataSizeField = true;

            BlockDataEntry = blockDataEntry;
            DiskLocations = diskLocations;
            DataSize = compressedDataBlockSize;
        }

        public WriteDescriptor(BlockDataEntry blockDataEntry, List<DiskLocation> diskLocations, FFUVersion ffuVersion)
        {
            HasDataSizeField = false;

            BlockDataEntry = blockDataEntry;
            DiskLocations = diskLocations;
        }

        public override string ToString()
        {
            return $"{{BlockDataEntry: {BlockDataEntry}}}";
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = [];

            BlockDataEntry.LocationCount = (uint)DiskLocations.Count;

            bytes.AddRange(BlockDataEntry.GetBytes());

            if (HasDataSizeField)
            {
                bytes.AddRange(BitConverter.GetBytes(DataSize));
            }

            foreach (DiskLocation diskLocation in DiskLocations)
            {
                bytes.AddRange(diskLocation.GetBytes());
            }

            return [.. bytes];
        }
    }
}