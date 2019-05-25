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
using System.Linq;

namespace Img2Ffu
{
    public class FlashingPayload
    {
        public UInt32 ChunkCount;
        public byte[][] ChunkHashes;
        public UInt32[] TargetLocations;
        public UInt32[] StreamIndexes;
        public Int64[] StreamLocations;
        public bool BeforePlat;

        public FlashingPayload(UInt32 ChunkCount, byte[][] ChunkHashes, UInt32[] TargetLocations, UInt32[] StreamIndexes, Int64[] StreamLocations, bool BeforePlat)
        {
            this.ChunkCount = ChunkCount;
            this.ChunkHashes = ChunkHashes;
            this.TargetLocations = TargetLocations;
            this.StreamIndexes = StreamIndexes;
            this.StreamLocations = StreamLocations;
            this.BeforePlat = BeforePlat;
        }

        public UInt32 GetSecurityHeaderSize()
        {
            return 0x20 * (UInt32)ChunkHashes.Count();
        }

        public UInt32 GetStoreHeaderSize()
        {
            return 0x08 * ((UInt32)TargetLocations.Count() + 1);
        }
    }
}