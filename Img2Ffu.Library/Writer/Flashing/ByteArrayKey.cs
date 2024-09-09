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
using System.Diagnostics.CodeAnalysis;

namespace Img2Ffu.Writer.Flashing
{
    public readonly struct ByteArrayKey
    {
        public readonly byte[] Bytes;
        private readonly int _hashCode;

        public override bool Equals(object obj)
        {
            ByteArrayKey other = (ByteArrayKey)obj;
            return Compare(Bytes, other.Bytes);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private static int GetHashCode([NotNull] byte[] bytes)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash = (hash * 23) + bytes[i];
                }
                return hash;
            }
        }

        public ByteArrayKey(byte[] bytes)
        {
            Bytes = bytes;
            _hashCode = GetHashCode(bytes);
        }

        public static ByteArrayKey Create(byte[] bytes)
        {
            return new ByteArrayKey(bytes);
        }

        public static unsafe bool Compare(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
            {
                return false;
            }

            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                {
                    if (*(long*)x1 != *(long*)x2)
                    {
                        return false;
                    }
                }

                if ((l & 4) != 0)
                {
                    if (*(int*)x1 != *(int*)x2)
                    {
                        return false;
                    }

                    x1 += 4;
                    x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*(short*)x1 != *(short*)x2)
                    {
                        return false;
                    }

                    x1 += 2;
                    x2 += 2;
                }
                if ((l & 1) != 0)
                {
                    if (*x1 != *x2)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static bool operator ==(ByteArrayKey left, ByteArrayKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ByteArrayKey left, ByteArrayKey right)
        {
            return !(left == right);
        }
    }
}