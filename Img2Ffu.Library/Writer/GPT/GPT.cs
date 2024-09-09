// Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using Img2Ffu.Writer.Helpers;

namespace Img2Ffu
{
    public partial class GPT
    {
        public byte[] GPTBuffer;
        private readonly uint HeaderOffset;
        private readonly uint HeaderSize;
        private uint TableOffset;
        private uint TableSize;
        private readonly uint PartitionEntrySize;
        private readonly uint MaxPartitions;
        internal ulong FirstUsableSector;
        internal ulong LastUsableSector;
        internal bool HasChanged = false;

        public List<Partition> Partitions = [];

        internal static uint GetGPTSize(byte[] GPTBuffer, uint SectorSize)
        {
            uint? TempHeaderOffset = ByteOperations.FindAscii(GPTBuffer, "EFI PART") ?? throw new Exception("Bad GPT");

            uint HeaderOffset = (uint)TempHeaderOffset;
            uint TableOffset = HeaderOffset + SectorSize;
            uint MaxPartitions = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x50);
            uint PartitionEntrySize = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x54);
            uint TableSize = MaxPartitions * PartitionEntrySize;

            return TableOffset + TableSize;
        }

        internal GPT(byte[] GPTBuffer, uint SectorSize)
        {
            this.GPTBuffer = GPTBuffer;
            uint? TempHeaderOffset = ByteOperations.FindAscii(GPTBuffer, "EFI PART") ?? throw new Exception("Bad GPT");

            HeaderOffset = (uint)TempHeaderOffset;
            HeaderSize = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x0C);
            TableOffset = HeaderOffset + SectorSize;
            FirstUsableSector = ByteOperations.ReadUInt64(GPTBuffer, HeaderOffset + 0x28);
            LastUsableSector = ByteOperations.ReadUInt64(GPTBuffer, HeaderOffset + 0x30);
            MaxPartitions = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x50);
            PartitionEntrySize = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x54);
            TableSize = MaxPartitions * PartitionEntrySize;
            if ((TableOffset + TableSize) > GPTBuffer.Length)
            {
                throw new Exception("Bad GPT");
            }

            uint PartitionOffset = TableOffset;

            while (PartitionOffset < (TableOffset + TableSize))
            {
                string Name = ByteOperations.ReadUnicodeString(GPTBuffer, PartitionOffset + 0x38, 0x48).TrimEnd([(char)0, ' ']);
                if (Name.Length == 0)
                {
                    break;
                }

                Partition CurrentPartition = new()
                {
                    Name = Name,
                    FirstSector = ByteOperations.ReadUInt64(GPTBuffer, PartitionOffset + 0x20),
                    LastSector = ByteOperations.ReadUInt64(GPTBuffer, PartitionOffset + 0x28),
                    PartitionTypeGuid = ByteOperations.ReadGuid(GPTBuffer, PartitionOffset + 0x00),
                    PartitionGuid = ByteOperations.ReadGuid(GPTBuffer, PartitionOffset + 0x10),
                    Attributes = ByteOperations.ReadUInt64(GPTBuffer, PartitionOffset + 0x30)
                };
                Partitions.Add(CurrentPartition);
                PartitionOffset += PartitionEntrySize;
            }

            HasChanged = false;
        }

        internal Partition GetPartition(string Name)
        {
            return Partitions.Where(p => string.Compare(p.Name, Name, true) == 0).FirstOrDefault();
        }

        internal byte[] Rebuild(uint SectorSize)
        {
            if (GPTBuffer == null)
            {
                TableSize = 0x21 * SectorSize;
                TableOffset = 0;
                GPTBuffer = new byte[TableSize];
            }
            else
            {
                Array.Clear(GPTBuffer, (int)TableOffset, (int)TableSize);
            }

            uint PartitionOffset = TableOffset;
            foreach (Partition CurrentPartition in Partitions)
            {
                ByteOperations.WriteGuid(GPTBuffer, PartitionOffset + 0x00, CurrentPartition.PartitionTypeGuid);
                ByteOperations.WriteGuid(GPTBuffer, PartitionOffset + 0x10, CurrentPartition.PartitionGuid);
                ByteOperations.WriteUInt64(GPTBuffer, PartitionOffset + 0x20, CurrentPartition.FirstSector);
                ByteOperations.WriteUInt64(GPTBuffer, PartitionOffset + 0x28, CurrentPartition.LastSector);
                ByteOperations.WriteUInt64(GPTBuffer, PartitionOffset + 0x30, CurrentPartition.Attributes);
                ByteOperations.WriteUnicodeString(GPTBuffer, PartitionOffset + 0x38, CurrentPartition.Name, 0x48);

                PartitionOffset += PartitionEntrySize;
            }

            ByteOperations.WriteUInt32(GPTBuffer, HeaderOffset + 0x58, ByteOperations.CRC32(GPTBuffer, TableOffset, TableSize));
            ByteOperations.WriteUInt32(GPTBuffer, HeaderOffset + 0x10, 0);
            ByteOperations.WriteUInt32(GPTBuffer, HeaderOffset + 0x10, ByteOperations.CRC32(GPTBuffer, HeaderOffset, HeaderSize));

            return GPTBuffer;
        }
    }
}