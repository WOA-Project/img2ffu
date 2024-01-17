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
using Img2Ffu.Data;
using System.IO;

namespace Img2Ffu.Flashing
{
    public class BlockPayload(WriteDescriptor WriteDescriptor, ulong StreamIndex, ulong StreamLocation)
    {
        public WriteDescriptor WriteDescriptor = WriteDescriptor;
        public ulong FlashPartIndex = StreamIndex;
        public ulong FlashPartStreamLocation = StreamLocation;

        internal byte[] ReadBlock(FlashPart[] flashParts, ulong BlockSize)
        {
            FlashPart FlashPart = flashParts[FlashPartIndex];
            Stream FlashPartStream = FlashPart.Stream;
            _ = FlashPartStream.Seek((long)FlashPartStreamLocation, SeekOrigin.Begin);

            byte[] BlockBuffer = new byte[BlockSize];
            _ = FlashPartStream.Read(BlockBuffer, 0, (int)BlockSize);

            return BlockBuffer;
        }
    }
}