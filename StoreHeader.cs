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
using System;

namespace Img2Ffu
{
    public class StoreHeader
    {
        public UInt32 UpdateType = 0; // Full update

        public UInt16 MajorVersion = 1;
        public UInt16 MinorVersion = 0;
        public UInt16 FullFlashMajorVersion = 2;
        public UInt16 FullFlashMinorVersion = 0;

        // Size is 0xC0
        public string PlatformId;

        public UInt32 BlockSizeInBytes = 131072;
        public UInt32 WriteDescriptorCount;
        public UInt32 WriteDescriptorLength;
        public UInt32 ValidateDescriptorCount = 0;
        public UInt32 ValidateDescriptorLength = 0;
        public UInt32 InitialTableIndex = 0;
        public UInt32 InitialTableCount = 1;
        public UInt32 FlashOnlyTableIndex = 0; // Should be the index of the critical partitions, but for now we don't implement that
        public UInt32 FlashOnlyTableCount = 1;
        public UInt32 FinalTableIndex; //= WriteDescriptorCount - FinalTableCount;
        public UInt32 FinalTableCount = 2;
    }
}
