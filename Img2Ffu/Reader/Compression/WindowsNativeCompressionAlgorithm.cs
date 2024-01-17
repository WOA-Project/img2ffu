using System;

namespace Img2Ffu.Reader.Compression
{
    [Flags]
    public enum WindowsNativeCompressionAlgorithm
    {
        COMPRESSION_FORMAT_NONE = 0x0000,
        COMPRESSION_ENGINE_STANDARD = 0x0000,
        COMPRESSION_FORMAT_DEFAULT = 0x0001,
        COMPRESSION_FORMAT_LZNT1 = 0x0002,
        COMPRESSION_FORMAT_XPRESS = 0x0003,
        COMPRESSION_FORMAT_XPRESS_HUFF = 0x0004,
        COMPRESSION_ENGINE_MAXIMUM = 0x0100,
        COMPRESSION_ENGINE_HIBER = 0x0200
    }
}
