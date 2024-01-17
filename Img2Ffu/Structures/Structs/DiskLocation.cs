using System.Runtime.InteropServices;

namespace Img2Ffu.Structures.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DiskLocation
    {
        public uint dwDiskAccessMethod;
        public uint dwBlockIndex;

        public override readonly string ToString() => $"{{dwDiskAccessMethod: {dwDiskAccessMethod}, dwBlockIndex: {dwBlockIndex}}}";
    }
}