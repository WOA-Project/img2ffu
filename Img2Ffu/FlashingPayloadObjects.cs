using System;

namespace Img2Ffu
{
    public class FlashingPayloadDescriptor
    {
        public UInt32 LocationCount;
        public UInt32 ChunkCount;
        public FlashingPayloadDataDescriptor[] flashingPayloadDataDescriptors;
    }

    public class FlashingPayloadDataDescriptor
    {
        public UInt32 DiskAccessMethod;
        public UInt32 ChunkIndex;
    }
}
