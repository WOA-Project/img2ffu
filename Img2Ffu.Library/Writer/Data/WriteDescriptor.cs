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
namespace Img2Ffu.Writer.Data
{
    public class WriteDescriptor
    {
        public required BlockDataEntry BlockDataEntry
        {
            get; set;
        }
        public required DiskLocation[] DiskLocations
        {
            get; set;
        }

        public Span<byte> GetResultingBuffer(FlashUpdateVersion storeHeaderVersion, uint CompressedDataBlockSize = 0)
        {
            using MemoryStream WriteDescriptorStream = new();
            using BinaryWriter binaryWriter = new(WriteDescriptorStream);

            BlockDataEntry.LocationCount = (uint)DiskLocations.Length;

            binaryWriter.Write(BlockDataEntry.LocationCount);
            binaryWriter.Write(BlockDataEntry.BlockCount);

            switch (storeHeaderVersion)
            {
                case FlashUpdateVersion.V1_COMPRESSED:
                    binaryWriter.Write(CompressedDataBlockSize);
                    break;
            }

            foreach (DiskLocation DiskLocation in DiskLocations)
            {
                binaryWriter.Write(DiskLocation.DiskAccessMethod);
                binaryWriter.Write(DiskLocation.BlockIndex);
            }

            Memory<byte> WriteDescriptorBuffer = new byte[WriteDescriptorStream.Length];
            Span<byte> span = WriteDescriptorBuffer.Span;
            _ = WriteDescriptorStream.Seek(0, SeekOrigin.Begin);
            WriteDescriptorStream.ReadExactly(span);

            return span;
        }
    }
}
