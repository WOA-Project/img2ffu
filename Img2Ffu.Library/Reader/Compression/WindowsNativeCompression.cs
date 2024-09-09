using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Compression
{
    internal class WindowsNativeCompression
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
