using System.Runtime.InteropServices;

namespace Img2Ffu.Structures.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct ImageHeader
    {
        public uint CbSize;           // sizeof(ImageHeader)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string Signature;      // "ImageFlash  "
        public uint ManifestLength;   // in bytes
        public uint DwChunkSize;      // Used only during image generation.
    }
}