using System.Runtime.InteropServices;

namespace Img2Ffu.Structures.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockDataEntry
    {
        public uint dwLocationCount;
        public uint dwBlockCount;

        public override readonly string ToString() => $"{{dwLocationCount: {dwLocationCount}, dwBlockCount: {dwBlockCount}}}";
    }
}
