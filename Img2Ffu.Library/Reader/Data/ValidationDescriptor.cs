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
using Img2Ffu.Reader.Structs;

namespace Img2Ffu.Reader.Data
{
    internal class ValidationDescriptor
    {
        public ValidationEntry ValidationEntry;
        public byte[] ValidationBytes;

        public ValidationDescriptor(Stream stream)
        {
            ValidationEntry = stream.ReadStructure<ValidationEntry>();

            ValidationBytes = new byte[ValidationEntry.dwByteCount];
            _ = stream.Read(ValidationBytes, 0, (int)ValidationEntry.dwByteCount);
        }

        public ValidationDescriptor(ValidationEntry validationEntry, byte[] validationBytes)
        {
            ValidationEntry = validationEntry;
            ValidationBytes = validationBytes;
            ValidationEntry.dwByteCount = (uint)validationBytes.LongLength;
        }

        public override string ToString()
        {
            return $"{{ValidationEntry: {ValidationEntry}}}";
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = [];

            ValidationEntry.dwByteCount = (uint)ValidationBytes.Length;

            bytes.AddRange(ValidationEntry.GetBytes());
            bytes.AddRange(ValidationBytes);

            return [.. bytes];
        }
    }
}
