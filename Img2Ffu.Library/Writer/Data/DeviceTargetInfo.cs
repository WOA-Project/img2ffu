/*
 * Copyright (c) Gustave Monce and Contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System.Text;

namespace Img2Ffu.Writer.Data
{
    public class DeviceTargetInfo(string manufacturer, string family, string productName, string productVersion, string skuNumber, string baseboardManufacturer, string baseboardProduct)
    {
        private uint ManufacturerLength;

        private uint FamilyLength;

        private uint ProductNameLength;

        private uint ProductVersionLength;

        private uint SKUNumberLength;

        private uint BaseboardManufacturerLength;

        private uint BaseboardProductLength;

        public string Manufacturer { get; } = manufacturer;

        public string Family { get; } = family;

        public string ProductName { get; } = productName;

        public string ProductVersion { get; } = productVersion;

        public string SKUNumber { get; } = skuNumber;

        public string BaseboardManufacturer { get; } = baseboardManufacturer;

        public string BaseboardProduct { get; } = baseboardProduct;

        internal Span<byte> GetResultingBuffer()
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

            Memory<byte> DeviceTargetInfoBuffer = new byte[DeviceTargetInfoStream.Length];
            Span<byte> span = DeviceTargetInfoBuffer.Span;
            _ = DeviceTargetInfoStream.Seek(0, SeekOrigin.Begin);
            DeviceTargetInfoStream.ReadExactly(span);

            return span;
        }
    }
}
