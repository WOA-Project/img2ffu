/*

Copyright (c) 2019, Gustave Monce - gus33000.me - @gus33000

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
using System.Text;

namespace Img2Ffu.Writer.Data
{
    internal class ImageHeader
    {
        public uint ManifestLength
        {
            get; set;
        }

        public byte[] GetResultingBuffer(uint BlockSize, bool HasDeviceTargetInfo, uint DeviceTargetInfosCount)
        {
            using MemoryStream ImageHeaderStream = new();
            using BinaryWriter binaryWriter = new(ImageHeaderStream);

            binaryWriter.Write(HasDeviceTargetInfo ? 28u : 24u); // Size
            binaryWriter.Write(Encoding.ASCII.GetBytes("ImageFlash  ")); // Signature
            binaryWriter.Write(ManifestLength); // Manifest Length
            binaryWriter.Write(BlockSize / 1024); // Chunk Size in KB

            if (HasDeviceTargetInfo)
            {
                binaryWriter.Write(DeviceTargetInfosCount); // Device Target Infos Count
            }

            byte[] ImageHeaderBuffer = new byte[ImageHeaderStream.Length];
            _ = ImageHeaderStream.Seek(0, SeekOrigin.Begin);
            ImageHeaderStream.ReadExactly(ImageHeaderBuffer, 0, ImageHeaderBuffer.Length);

            return ImageHeaderBuffer;
        }
    }
}
