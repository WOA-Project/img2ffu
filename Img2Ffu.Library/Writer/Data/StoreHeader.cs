/*

Copyright (c) 2019, Gustave Monce - gus33000.me - @gus33000

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
using System.Text;

namespace Img2Ffu.Writer.Data
{
    public class StoreHeader
    {
        private uint UpdateType = 0; // Full update

        private ushort MajorVersion = 1;
        private ushort MinorVersion = 0;
        private ushort FullFlashMajorVersion = 2;
        private ushort FullFlashMinorVersion = 0;

        // Size is 0xC0
        public required string[] PlatformIds
        {
            get; set;
        }

        public uint BlockSize
        {
            get; set;
        }
        public uint WriteDescriptorCount
        {
            get; set;
        }
        public uint WriteDescriptorLength
        {
            get; set;
        }
        public uint ValidateDescriptorCount { get; set; } = 0;
        public uint ValidateDescriptorLength { get; set; } = 0;
        public uint InitialTableIndex { get; set; } = 0;
        public uint InitialTableCount { get; set; } = 0;
        public uint FlashOnlyTableIndex { get; set; } = 0; // Should be the index of the critical partitions, but for now we don't implement that
        public uint FlashOnlyTableCount { get; set; } = 1;

        private uint FinalTableIndex; //= WriteDescriptorCount - FinalTableCount;

        public uint FinalTableCount { get; set; } = 0;

        // V1 Compressed
        private uint CompressionAlgorithm = 0; // 0: None, 1: GZip

        // V2
        public ushort NumberOfStores
        {
            get; set;
        } // 0x4 (Total number of stores)
        public ushort StoreIndex
        {
            get; set;
        } // 0x1 (Starts counting from 1)
        public ulong StorePayloadSize
        {
            get; set;
        } // 0x420000

        private ushort DevicePathLength; // 0x2b
        // Must be followed by the unicode string of the device path
        // So the total size would be doubled from DevicePathLength in bytes in the binary

        private byte[] DevicePathBuffer;

        public required string DevicePath
        {
            get; set;
        }

        public byte[] GetResultingBuffer(FlashUpdateVersion storeHeaderVersion, FlashUpdateType storeHeaderUpdateType, CompressionAlgorithm storeHeaderCompressionAlgorithm)
        {
            switch (storeHeaderVersion)
            {
                case FlashUpdateVersion.V1:
                    MajorVersion = 1;
                    MinorVersion = 0;
                    FullFlashMajorVersion = 2;
                    FullFlashMinorVersion = 0;
                    break;
                case FlashUpdateVersion.V1_COMPRESSED:
                    MajorVersion = 1;
                    MinorVersion = 0;
                    FullFlashMajorVersion = 3;
                    FullFlashMinorVersion = 0;
                    CompressionAlgorithm = (uint)storeHeaderCompressionAlgorithm;
                    break;
                case FlashUpdateVersion.V2:
                    MajorVersion = 2;
                    MinorVersion = 0;
                    FullFlashMajorVersion = 2;
                    FullFlashMinorVersion = 0;
                    UnicodeEncoding UnicodeEncoding = new();
                    DevicePathBuffer = UnicodeEncoding.GetBytes(DevicePath.ToCharArray());
                    DevicePathLength = (ushort)DevicePath.Length;
                    break;
            }

            switch (storeHeaderUpdateType)
            {
                case FlashUpdateType.Full:
                    UpdateType = 0;
                    break;
                case FlashUpdateType.Partial:
                    UpdateType = 1;
                    break;
            }

            byte[] PlatformIdsBuffer = new byte[192];
            int CurrentBufferSize = 0;
            foreach (string PlatformId in PlatformIds)
            {
                byte[] PlatformIdBuffer = Encoding.ASCII.GetBytes(PlatformId);
                int AddedBufferSize = PlatformIdBuffer.Length + 1;
                if (CurrentBufferSize + AddedBufferSize > PlatformIdsBuffer.Length - 1)
                {
                    break;
                }
                PlatformIdBuffer.CopyTo(PlatformIdsBuffer, CurrentBufferSize);
                CurrentBufferSize += AddedBufferSize;
            }

            FinalTableIndex = WriteDescriptorCount - FinalTableCount;

            using MemoryStream StoreHeaderStream = new();
            using BinaryWriter binaryWriter = new(StoreHeaderStream);

            binaryWriter.Write(UpdateType);
            binaryWriter.Write(MajorVersion);
            binaryWriter.Write(MinorVersion);
            binaryWriter.Write(FullFlashMajorVersion);
            binaryWriter.Write(FullFlashMinorVersion);
            binaryWriter.Write(PlatformIdsBuffer);
            binaryWriter.Write(BlockSize);
            binaryWriter.Write(WriteDescriptorCount);
            binaryWriter.Write(WriteDescriptorLength);
            binaryWriter.Write(ValidateDescriptorCount);
            binaryWriter.Write(ValidateDescriptorLength);
            binaryWriter.Write(InitialTableIndex);
            binaryWriter.Write(InitialTableCount);
            binaryWriter.Write(FlashOnlyTableIndex);
            binaryWriter.Write(FlashOnlyTableCount);
            binaryWriter.Write(FinalTableIndex);
            binaryWriter.Write(FinalTableCount);

            switch (storeHeaderVersion)
            {
                case FlashUpdateVersion.V1_COMPRESSED:
                    binaryWriter.Write(CompressionAlgorithm);
                    break;
                case FlashUpdateVersion.V2:
                    binaryWriter.Write(NumberOfStores);
                    binaryWriter.Write(StoreIndex);
                    binaryWriter.Write(StorePayloadSize);
                    binaryWriter.Write(DevicePathLength);
                    binaryWriter.Write(DevicePathBuffer);
                    break;
            }

            byte[] StoreHeaderBuffer = new byte[StoreHeaderStream.Length];
            _ = StoreHeaderStream.Seek(0, SeekOrigin.Begin);
            StoreHeaderStream.ReadExactly(StoreHeaderBuffer, 0, StoreHeaderBuffer.Length);

            return StoreHeaderBuffer;
        }
    }
}
