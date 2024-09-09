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
    public class SecurityHeader
    {
        private readonly uint Size = 32;
        private readonly string Signature = "SignedImage ";
        private readonly uint HashAlgorithm = 0x800C; // SHA256 algorithm id

        public uint CatalogSize;
        public uint HashTableSize;

        public byte[] GetResultingBuffer(uint BlockSize)
        {
            using MemoryStream SecurityHeaderStream = new();
            using BinaryWriter binaryWriter = new(SecurityHeaderStream);

            uint ChunkSizeInKb = BlockSize / 1024;

            binaryWriter.Write(Size);
            binaryWriter.Write(Encoding.ASCII.GetBytes(Signature));
            binaryWriter.Write(ChunkSizeInKb);
            binaryWriter.Write(HashAlgorithm);
            binaryWriter.Write(CatalogSize);
            binaryWriter.Write(HashTableSize);

            byte[] SecurityHeaderBuffer = new byte[SecurityHeaderStream.Length];
            _ = SecurityHeaderStream.Seek(0, SeekOrigin.Begin);
            SecurityHeaderStream.ReadExactly(SecurityHeaderBuffer, 0, SecurityHeaderBuffer.Length);

            return SecurityHeaderBuffer;
        }
    }
}
