using System.IO;
using System.Text;

namespace Img2Ffu.Writer.Data
{
    internal class DeviceTargetInfo(string manufacturer, string family, string productName, string productVersion, string sKUNumber, string baseboardManufacturer, string baseboardProduct)
    {
        private uint ManufacturerLength;

        private uint FamilyLength;

        private uint ProductNameLength;

        private uint ProductVersionLength;

        private uint SKUNumberLength;

        private uint BaseboardManufacturerLength;

        private uint BaseboardProductLength;

        public string Manufacturer = manufacturer;

        public string Family = family;

        public string ProductName = productName;

        public string ProductVersion = productVersion;

        public string SKUNumber = sKUNumber;

        public string BaseboardManufacturer = baseboardManufacturer;

        public string BaseboardProduct = baseboardProduct;

        public byte[] GetResultingBuffer()
        {
            using MemoryStream DeviceTargetInfoStream = new();
            using BinaryWriter binaryWriter = new(DeviceTargetInfoStream);

            ManufacturerLength = (uint)Manufacturer.Length;
            FamilyLength = (uint)Family.Length;
            ProductNameLength = (uint)ProductName.Length;
            ProductVersionLength = (uint)ProductVersion.Length;
            SKUNumberLength = (uint)SKUNumber.Length;
            BaseboardManufacturerLength = (uint)BaseboardManufacturer.Length;
            BaseboardProductLength = (uint)BaseboardProduct.Length;

            binaryWriter.Write(ManufacturerLength);
            binaryWriter.Write(FamilyLength);
            binaryWriter.Write(ProductNameLength);
            binaryWriter.Write(ProductVersionLength);
            binaryWriter.Write(SKUNumberLength);
            binaryWriter.Write(BaseboardManufacturerLength);
            binaryWriter.Write(BaseboardProductLength);

            binaryWriter.Write(Encoding.ASCII.GetBytes(Manufacturer));
            binaryWriter.Write(Encoding.ASCII.GetBytes(Family));
            binaryWriter.Write(Encoding.ASCII.GetBytes(ProductName));
            binaryWriter.Write(Encoding.ASCII.GetBytes(ProductVersion));
            binaryWriter.Write(Encoding.ASCII.GetBytes(SKUNumber));
            binaryWriter.Write(Encoding.ASCII.GetBytes(BaseboardManufacturer));
            binaryWriter.Write(Encoding.ASCII.GetBytes(BaseboardProduct));

            byte[] DeviceTargetInfoBuffer = new byte[DeviceTargetInfoStream.Length];
            _ = DeviceTargetInfoStream.Seek(0, SeekOrigin.Begin);
            DeviceTargetInfoStream.ReadExactly(DeviceTargetInfoBuffer, 0, DeviceTargetInfoBuffer.Length);

            return DeviceTargetInfoBuffer;
        }
    }
}
