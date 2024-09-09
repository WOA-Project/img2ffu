using Img2Ffu.Reader.Enums;
using Img2Ffu.Reader.Structs;
using System.Text;

namespace Img2Ffu.Reader.Data
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
        public readonly FFUVersion FFUVersion;

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

            FFUVersion = GetFFUVersion();

            switch (FFUVersion)
            {
                case FFUVersion.V2:
                    {
                        StoreHeaderV2 = stream.ReadStructure<StoreHeaderV2>();
                        byte[] stringBytes = new byte[StoreHeaderV2.DevicePathLength * 2];
                        _ = stream.Read(stringBytes, 0, stringBytes.Length);
                        UnicodeEncoding unicodeEncoding = new();
                        DevicePath = unicodeEncoding.GetString(stringBytes);
                        break;
                    }
                case FFUVersion.V1_COMPRESSED:
                    {
                        using BinaryReader binaryReader = new(stream, Encoding.ASCII, true);
                        CompressionAlgorithm = binaryReader.ReadUInt32();
                        break;
                    }
            }

            for (uint i = 0; i < StoreHeader.ValidateDescriptorCount; i++)
            {
                ValidationDescriptor.Add(new ValidationDescriptor(stream));
            }

            for (uint i = 0; i < StoreHeader.WriteDescriptorCount; i++)
            {
                WriteDescriptors.Add(new WriteDescriptor(stream, FFUVersion));
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

        private FFUVersion GetFFUVersion()
        {
            return (StoreHeader.MajorVersion, StoreHeader.FullFlashMajorVersion) switch
            {
                (1, 2) => FFUVersion.V1,
                (1, 3) => FFUVersion.V1_COMPRESSED,
                (2, 2) => FFUVersion.V2,
                _ => throw new InvalidDataException($"Unsupported FFU Store Format! MajorVersion: {StoreHeader.MajorVersion} FullFlashMajorVersion: {StoreHeader.FullFlashMajorVersion}"),
            };
        }
    }
}
