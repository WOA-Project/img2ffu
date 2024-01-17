using System.Runtime.InteropServices;

namespace Img2Ffu.Structures.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StoreHeader
    {
        public uint dwUpdateType; // indicates partial or full flash
        public ushort MajorVersion, MinorVersion; // used to validate struct
        public ushort FullFlashMajorVersion, FullFlashMinorVersion; // FFU version, i.e. the image format
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 192)]
        public string szPlatformId; // string which indicates what device this FFU is intended to be written to
        public uint dwBlockSizeInBytes; // size of an image block in bytes – the device’s actual sector size may differ
        public uint dwWriteDescriptorCount; // number of write descriptors to iterate through
        public uint dwWriteDescriptorLength; // total size of all the write descriptors, in bytes (included so they can be read out up front and interpreted later)
        public uint dwValidateDescriptorCount; // number of validation descriptors to check
        public uint dwValidateDescriptorLength; // total size of all the validation descriptors, in bytes
        public uint dwInitialTableIndex; // block index in the payload of the initial (invalid) GPT
        public uint dwInitialTableCount; // count of blocks for the initial GPT, i.e. the GPT spans blockArray[idx..(idx + count -1)]
        public uint dwFlashOnlyTableIndex; // first block index in the payload of the flash-only GPT (included so safe flashing can be accomplished)
        public uint dwFlashOnlyTableCount; // count of blocks in the flash-only GPT
        public uint dwFinalTableIndex; // index in the table of the real GPT
        public uint dwFinalTableCount; // number of blocks in the real GPT

        public override readonly string ToString() => $"{{dwUpdateType: {dwUpdateType}, MajorVersion: {MajorVersion}, MinorVersion: {MinorVersion}}}";
    }
}