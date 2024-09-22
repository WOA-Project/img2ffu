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
using System.Runtime.InteropServices;

namespace Img2Ffu.Reader.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StoreHeader
    {
        public uint UpdateType; // indicates partial or full flash
        public ushort MajorVersion, MinorVersion; // used to validate struct
        public ushort FullFlashMajorVersion, FullFlashMinorVersion; // FFU version, i.e. the image format
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
        public byte[] PlatformId; // string which indicates what device this FFU is intended to be written to
        public uint BlockSize; // size of an image block in bytes – the device’s actual sector size may differ
        public uint WriteDescriptorCount; // number of write descriptors to iterate through
        public uint WriteDescriptorLength; // total size of all the write descriptors, in bytes (included so they can be read out up front and interpreted later)
        public uint ValidateDescriptorCount; // number of validation descriptors to check
        public uint ValidateDescriptorLength; // total size of all the validation descriptors, in bytes
        public uint InitialTableIndex; // block index in the payload of the initial (invalid) GPT
        public uint InitialTableCount; // count of blocks for the initial GPT, i.e. the GPT spans blockArray[idx..(idx + count -1)]
        public uint FlashOnlyTableIndex; // first block index in the payload of the flash-only GPT (included so safe flashing can be accomplished)
        public uint FlashOnlyTableCount; // count of blocks in the flash-only GPT
        public uint FinalTableIndex; // index in the table of the real GPT
        public uint FinalTableCount; // number of blocks in the real GPT

        public override readonly string ToString()
        {
            return $"{{UpdateType: {UpdateType}, MajorVersion: {MajorVersion}, MinorVersion: {MinorVersion}}}";
        }
    }
}