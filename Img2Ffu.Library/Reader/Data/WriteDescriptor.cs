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
using Img2Ffu.Reader.Enums;
using Img2Ffu.Reader.Structs;
using System.Text;

namespace Img2Ffu.Reader.Data
{
    public class WriteDescriptor
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