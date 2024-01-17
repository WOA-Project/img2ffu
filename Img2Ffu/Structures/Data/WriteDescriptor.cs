using Img2Ffu.Structures.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Img2Ffu.Structures.Data
{
    internal class WriteDescriptor
    {
        public BlockDataEntry BlockDataEntry;
        public uint DataSize;
        public readonly List<DiskLocation> DiskLocations = [];
        public bool IsFFUV1_3 = false;

        public WriteDescriptor(Stream stream, bool isFFUV1_3)
        {
            IsFFUV1_3 = isFFUV1_3;

            BlockDataEntry = stream.ReadStructure<BlockDataEntry>();

            if (isFFUV1_3)
            {
                using BinaryReader binaryReader = new(stream, Encoding.ASCII, true);
                DataSize = binaryReader.ReadUInt32();
            }

            for (uint i = 0; i < BlockDataEntry.LocationCount; i++)
            {
                DiskLocations.Add(stream.ReadStructure<DiskLocation>());
            }
        }

        public WriteDescriptor(BlockDataEntry blockDataEntry, List<DiskLocation> diskLocations, uint dataSize)
        {
            IsFFUV1_3 = true;

            BlockDataEntry = blockDataEntry;
            DiskLocations = diskLocations;
            DataSize = dataSize;
        }

        public WriteDescriptor(BlockDataEntry blockDataEntry, List<DiskLocation> diskLocations)
        {
            IsFFUV1_3 = false;

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

            if (IsFFUV1_3)
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