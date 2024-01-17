using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Img2Ffu.Structures.Structs;

namespace Img2Ffu.Structures.Data
{
    internal class WriteDescriptor
    {
        public BlockDataEntry BlockDataEntry;
        public uint CompressionAlgorithm;
        public readonly List<DiskLocation> DiskLocations = [];
        public bool IsFFUV1_3 = false;

        public WriteDescriptor(Stream stream, bool isFFUV1_3)
        {
            IsFFUV1_3 = isFFUV1_3;

            BlockDataEntry = stream.ReadStructure<BlockDataEntry>();

            if (isFFUV1_3)
            {
                using BinaryReader binaryReader = new(stream, Encoding.ASCII, true);
                CompressionAlgorithm = binaryReader.ReadUInt32();
            }

            for (uint i = 0; i < BlockDataEntry.dwLocationCount; i++)
            {
                DiskLocations.Add(stream.ReadStructure<DiskLocation>());
            }
        }

        public WriteDescriptor(BlockDataEntry blockDataEntry, List<DiskLocation> diskLocations, uint compressionAlgorithm)
        {
            IsFFUV1_3 = true;

            BlockDataEntry = blockDataEntry;
            DiskLocations = diskLocations;
            CompressionAlgorithm = compressionAlgorithm;
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

            BlockDataEntry.dwLocationCount = (uint)DiskLocations.Count;

            bytes.AddRange(BlockDataEntry.GetBytes());

            if (IsFFUV1_3)
            {
                bytes.AddRange(BitConverter.GetBytes(CompressionAlgorithm));
            }

            foreach (DiskLocation diskLocation in DiskLocations)
            {
                bytes.AddRange(diskLocation.GetBytes());
            }

            return [.. bytes];
        }
    }
}