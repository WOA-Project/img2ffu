using Img2Ffu.Structures.Structs;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Img2Ffu.Structures.Data
{
    internal class Store
    {
        public StoreHeader StoreHeader;
        public uint CompressionAlgorithm;
        public StoreHeaderV2 StoreHeaderV2;
        public string DevicePath = ""; // Device path has no NUL at then end (V2 only)
        public readonly List<ValidationDescriptor> ValidationDescriptor = [];
        public readonly List<WriteDescriptor> WriteDescriptors = [];
        public readonly List<string> PlatformIDs = [];
        public byte[] Padding = [];

        public Store(Stream stream)
        {
            StoreHeader = stream.ReadStructure<StoreHeader>();

            int lastFinding = 0;
            for (int i = 0; i < StoreHeader.PlatformId.Length; i++)
            {
                if (StoreHeader.PlatformId[i] == '\0')
                {
                    if (i == lastFinding)
                    {
                        break;
                    }

                    ASCIIEncoding asciiEnconding = new();
                    string PlatformID = asciiEnconding.GetString(StoreHeader.PlatformId[lastFinding..i]);
                    PlatformIDs.Add(PlatformID);
                    lastFinding = i + 1;
                }
            }

            bool isFFUV2_2 = StoreHeader.MajorVersion == 2 && StoreHeader.FullFlashMajorVersion == 2;
            bool isFFUV1_3 = StoreHeader.MajorVersion == 1 && StoreHeader.FullFlashMajorVersion == 3;
            bool isFFUV1_2 = StoreHeader.MajorVersion == 2 && StoreHeader.FullFlashMajorVersion == 2;

            if (isFFUV2_2)
            {
                StoreHeaderV2 = stream.ReadStructure<StoreHeaderV2>();
                byte[] stringBytes = new byte[StoreHeaderV2.DevicePathLength * 2];
                _ = stream.Read(stringBytes, 0, stringBytes.Length);
                UnicodeEncoding unicodeEncoding = new();
                DevicePath = unicodeEncoding.GetString(stringBytes);
            }
            else if (isFFUV1_3)
            {
                using BinaryReader binaryReader = new(stream, Encoding.ASCII, true);
                CompressionAlgorithm = binaryReader.ReadUInt32();
            }
            else if (isFFUV1_2)
            {
                throw new InvalidDataException($"Unsupported FFU Store Format! MajorVersion: {StoreHeader.MajorVersion} FullFlashMajorVersion: {StoreHeader.FullFlashMajorVersion}");
            }

            for (uint i = 0; i < StoreHeader.ValidateDescriptorCount; i++)
            {
                ValidationDescriptor.Add(new ValidationDescriptor(stream));
            }

            for (uint i = 0; i < StoreHeader.WriteDescriptorCount; i++)
            {
                WriteDescriptors.Add(new WriteDescriptor(stream, isFFUV1_3));
            }

            long Position = stream.Position;
            if (Position % StoreHeader.BlockSize > 0)
            {
                long paddingSize = StoreHeader.BlockSize - (Position % StoreHeader.BlockSize);
                Padding = new byte[paddingSize];
                _ = stream.Read(Padding, 0, (int)paddingSize);
            }
        }

        public override string ToString()
        {
            return $"{{StoreHeader: {StoreHeader}, StoreHeaderV2: {StoreHeaderV2}, DevicePath: {DevicePath}}}";
        }
    }
}
