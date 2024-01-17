using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockDataEntry
    {
        public uint LocationCount;
        public uint BlockCount;

        public override readonly string ToString()
        {
            return $"{{LocationCount: {LocationCount}, BlockCount: {BlockCount}}}";
        }
    }
}
