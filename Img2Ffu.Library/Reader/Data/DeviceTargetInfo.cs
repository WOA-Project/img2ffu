namespace Img2Ffu.Reader.Data
{
    public class DeviceTargetInfo
    {
        public uint ManufacturerLength;

        public uint FamilyLength;

        public uint ProductNameLength;

        public uint ProductVersionLength;

        public uint SKUNumberLength;

        public uint BaseboardManufacturerLength;

        public uint BaseboardProductLength;

        public string Manufacturer
        {
            get;
        }

        public string Family
        {
            get;
        }

        public string ProductName
        {
            get;
        }

        public string ProductVersion
        {
            get;
        }

        public string SKUNumber
        {
            get;
        }

        public string BaseboardManufacturer
        {
            get;
        }

        public string BaseboardProduct
        {
            get;
        }

        public DeviceTargetInfo(Stream stream)
        {
            using BinaryReader binaryReader = new(stream);

            ManufacturerLength = binaryReader.ReadUInt32();
            FamilyLength = binaryReader.ReadUInt32();
            ProductNameLength = binaryReader.ReadUInt32();
            ProductVersionLength = binaryReader.ReadUInt32();
            SKUNumberLength = binaryReader.ReadUInt32();
            BaseboardManufacturerLength = binaryReader.ReadUInt32();
            BaseboardProductLength = binaryReader.ReadUInt32();

            Manufacturer = new string(binaryReader.ReadChars((int)ManufacturerLength));
            Family = new string(binaryReader.ReadChars((int)FamilyLength));
            ProductName = new string(binaryReader.ReadChars((int)ProductNameLength));
            ProductVersion = new string(binaryReader.ReadChars((int)ProductVersionLength));
            SKUNumber = new string(binaryReader.ReadChars((int)SKUNumberLength));
            BaseboardManufacturer = new string(binaryReader.ReadChars((int)BaseboardManufacturerLength));
            BaseboardProduct = new string(binaryReader.ReadChars((int)BaseboardProductLength));
        }
    }
}
