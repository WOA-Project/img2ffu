using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DiskLocation
    {
        public uint DiskAccessMethod;
        public uint BlockIndex;

        public override readonly string ToString()
        {
            return $"{{DiskAccessMethod: {DiskAccessMethod}, BlockIndex: {BlockIndex}}}";
        }
    }
}