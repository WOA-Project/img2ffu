using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SecurityHeader
    {
        public uint Size;            // size of struct, overall
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string Signature;       // "SignedImage "
        public uint ChunkSizeInKB;   // size of a hashed chunk within the image
        public uint AlgorithmId;           // algorithm used to hash
        public uint CatalogSize;     // size of catalog to validate
        public uint HashTableSize;   // size of hash table
    }
}