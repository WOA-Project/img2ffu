using System.Runtime.InteropServices;

namespace Img2Ffu.Structures.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SecurityHeader
    {
        public uint CbSize;            // size of struct, overall
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string Signature;       // "SignedImage "
        public uint DwChunkSizeInKb;   // size of a hashed chunk within the image
        public uint DwAlgId;           // algorithm used to hash
        public uint DwCatalogSize;     // size of catalog to validate
        public uint DwHashTableSize;   // size of hash table
    }
}