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
