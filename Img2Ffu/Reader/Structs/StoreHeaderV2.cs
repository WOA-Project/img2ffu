using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StoreHeaderV2
    {
        public ushort NumberOfStores; // Total number of stores (V2 only)
        public ushort StoreIndex; // Current store index, 1-based (V2 only)
        public ulong StorePayloadSize; // Payload data only, excludes padding (V2 only)
        public ushort DevicePathLength; // Length of the device path (V2 only)

        public override readonly string ToString()
        {
            return $"{{NumberOfStores: {NumberOfStores}, StoreIndex: {StoreIndex}, StorePayloadSize: {StorePayloadSize}, DevicePathLength: {DevicePathLength}}}";
        }
    }
}