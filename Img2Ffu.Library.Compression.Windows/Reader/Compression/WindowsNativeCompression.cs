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
using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Compression
{
    public class WindowsNativeCompression
    {
        [DllImport("ntdll.dll")]
        private static extern uint RtlGetCompressionWorkSpaceSize(ushort CompressionFormatAndEngine, out uint CompressBufferWorkSpaceSize, out uint CompressFragmentWorkSpaceSize);

        [DllImport("ntdll.dll")]
        private static extern uint RtlDecompressBufferEx(ushort CompressionFormat, byte[] UncompressedBuffer, int UncompressedBufferSize, byte[] CompressedBuffer, int CompressedBufferSize, out int FinalUncompressedSize, byte[] WorkSpace);

        [DllImport("ntdll.dll")]
        private static extern uint RtlCompressBuffer(ushort CompressionFormatAndEngine, byte[] UncompressedBuffer, int UncompressedBufferSize, byte[] CompressedBuffer, int CompressedBufferSize, int UncompressedChunkSize, out int FinalCompressedSize, byte[] WorkSpace);

        public static byte[] Decompress(WindowsNativeCompressionAlgorithm CompressionFormat, byte[] CompressedBuffer, int UncompressedBufferSize)
        {
            uint ntStatus = RtlGetCompressionWorkSpaceSize((ushort)CompressionFormat, out uint compressBufferWorkSpaceSize, out _);
            if (ntStatus != 0)
            {
                throw new Exception($"Cannot get decompress workspace size! NTSTATUS=0x{ntStatus:X8}");
            }

            byte[] UncompressedBuffer = new byte[UncompressedBufferSize];
            byte[] workSpace = new byte[compressBufferWorkSpaceSize];


            ntStatus = RtlDecompressBufferEx((ushort)CompressionFormat, UncompressedBuffer, UncompressedBuffer.Length, CompressedBuffer, CompressedBuffer.Length, out _, workSpace);
            return ntStatus != 0 ? throw new Exception($"Cannot decompress buffer! NTSTATUS=0x{ntStatus:X8}") : UncompressedBuffer;
        }

        public static byte[] Compress(WindowsNativeCompressionAlgorithm CompressionFormat, byte[] UncompressedBuffer, int CompressedBufferSize)
        {
            uint ntStatus = RtlGetCompressionWorkSpaceSize((ushort)CompressionFormat, out uint compressBufferWorkSpaceSize, out _);
            if (ntStatus != 0)
            {
                throw new Exception($"Cannot get compression workspace size! NTSTATUS=0x{ntStatus:X8}");
            }

            byte[] CompressedBuffer = new byte[CompressedBufferSize];
            byte[] workSpace = new byte[compressBufferWorkSpaceSize];


            ntStatus = RtlCompressBuffer((ushort)CompressionFormat, UncompressedBuffer, UncompressedBuffer.Length, CompressedBuffer, CompressedBuffer.Length, 4096, out _, workSpace);
            return ntStatus != 0 ? throw new Exception($"Cannot compress buffer! NTSTATUS=0x{ntStatus:X8}") : CompressedBuffer;
        }
    }
}
