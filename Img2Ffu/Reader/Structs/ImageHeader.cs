using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct ImageHeader
    {
        public uint Size;           // sizeof(ImageHeader)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string Signature;      // "ImageFlash  "
        public uint ManifestLength;   // in bytes
        public uint ChunkSize;      // Used only during image generation.
    }
}