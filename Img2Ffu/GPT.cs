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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Img2Ffu
{
    public class GPT
    {
        private byte[] GPTBuffer;
        private UInt32 HeaderOffset;
        private UInt32 HeaderSize;
        private UInt32 TableOffset;
        private UInt32 TableSize;
        private UInt32 PartitionEntrySize;
        private UInt32 MaxPartitions;
        internal UInt64 FirstUsableSector;
        internal UInt64 LastUsableSector;
        internal bool HasChanged = false;
        
        public List<Partition> Partitions = new List<Partition>();
        
        internal GPT(byte[] GPTBuffer)
        {
            this.GPTBuffer = GPTBuffer;
            UInt32? TempHeaderOffset = ByteOperations.FindAscii(GPTBuffer, "EFI PART");
            if (TempHeaderOffset == null)
                throw new Exception("Bad GPT");
            HeaderOffset = (UInt32)TempHeaderOffset;
            HeaderSize = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x0C);
            TableOffset = HeaderOffset + 0x200;
            FirstUsableSector = ByteOperations.ReadUInt64(GPTBuffer, HeaderOffset + 0x28);
            LastUsableSector = ByteOperations.ReadUInt64(GPTBuffer, HeaderOffset + 0x30);
            MaxPartitions = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x50);
            PartitionEntrySize = ByteOperations.ReadUInt32(GPTBuffer, HeaderOffset + 0x54);
            TableSize = MaxPartitions * PartitionEntrySize;
            if ((TableOffset + TableSize) > GPTBuffer.Length)
                throw new Exception("Bad GPT");

            UInt32 PartitionOffset = TableOffset;

            while (PartitionOffset < (TableOffset + TableSize))
            {
                string Name = ByteOperations.ReadUnicodeString(GPTBuffer, PartitionOffset + 0x38, 0x48).TrimEnd(new char[] { (char)0, ' ' });
                if (Name.Length == 0)
                    break;
                Partition CurrentPartition = new Partition();
                CurrentPartition.Name = Name;
                CurrentPartition.FirstSector = ByteOperations.ReadUInt64(GPTBuffer, PartitionOffset + 0x20);
                CurrentPartition.LastSector = ByteOperations.ReadUInt64(GPTBuffer, PartitionOffset + 0x28);
                CurrentPartition.PartitionTypeGuid = ByteOperations.ReadGuid(GPTBuffer, PartitionOffset + 0x00);
                CurrentPartition.PartitionGuid = ByteOperations.ReadGuid(GPTBuffer, PartitionOffset + 0x10);
                CurrentPartition.Attributes = ByteOperations.ReadUInt64(GPTBuffer, PartitionOffset + 0x30);
                Partitions.Add(CurrentPartition);
                PartitionOffset += PartitionEntrySize;
            }

            HasChanged = false;
        }

        internal Partition GetPartition(string Name)
        {
            return Partitions.Where(p => (string.Compare(p.Name, Name, true) == 0)).FirstOrDefault();
        }

        internal byte[] Rebuild()
        {
            if (GPTBuffer == null)
            {
                TableSize = 0x4200;
                TableOffset = 0;
                GPTBuffer = new byte[TableSize];
            }
            else
                Array.Clear(GPTBuffer, (int)TableOffset, (int)TableSize);

            UInt32 PartitionOffset = TableOffset;
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

        public class Partition
        {
            private UInt64 _SizeInSectors;
            private UInt64 _FirstSector;
            private UInt64 _LastSector;

            public string Name;            // 0x48
            public Guid PartitionTypeGuid; // 0x10
            public Guid PartitionGuid;     // 0x10
            internal UInt64 Attributes;      // 0x08
            
            internal UInt64 SizeInSectors
            {
                get
                {
                    if (_SizeInSectors != 0)
                        return _SizeInSectors;
                    else
                        return LastSector - FirstSector + 1;
                }
                set
                {
                    _SizeInSectors = value;
                    if (FirstSector != 0)
                        LastSector = FirstSector + _SizeInSectors - 1;
                }
            }
            
            internal UInt64 FirstSector // 0x08
            {
                get
                {
                    return _FirstSector;
                }
                set
                {
                    _FirstSector = value;
                    if (_SizeInSectors != 0)
                        _LastSector = FirstSector + _SizeInSectors - 1;
                }
            }
            
            internal UInt64 LastSector // 0x08
            {
                get
                {
                    return _LastSector;
                }
                set
                {
                    _LastSector = value;
                    _SizeInSectors = 0;
                }
            }
            
            public string Volume
            {
                get
                {
                    return @"\\?\Volume" + PartitionGuid.ToString("b") + @"\";
                }
            }
            
            public string FirstSectorAsString
            {
                get
                {
                    return "0x" + FirstSector.ToString("X16");
                }
                set
                {
                    FirstSector = Convert.ToUInt64(value, 16);
                }
            }
            
            public string LastSectorAsString
            {
                get
                {
                    return "0x" + LastSector.ToString("X16");
                }
                set
                {
                    LastSector = Convert.ToUInt64(value, 16);
                }
            }
            
            public string AttributesAsString
            {
                get
                {
                    return "0x" + Attributes.ToString("X16");
                }
                set
                {
                    Attributes = Convert.ToUInt64(value, 16);
                }
            }
        }
    }
}