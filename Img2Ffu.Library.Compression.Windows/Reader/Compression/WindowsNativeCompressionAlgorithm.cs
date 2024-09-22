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
namespace Img2Ffu.Reader.Compression
{
    [Flags]
    public enum WindowsNativeCompressionAlgorithm
    {
        COMPRESSION_FORMAT_NONE = 0x0000,
        COMPRESSION_ENGINE_STANDARD = 0x0000,
        COMPRESSION_FORMAT_DEFAULT = 0x0001,
        COMPRESSION_FORMAT_LZNT1 = 0x0002,
        COMPRESSION_FORMAT_XPRESS = 0x0003,
        COMPRESSION_FORMAT_XPRESS_HUFF = 0x0004,
        COMPRESSION_ENGINE_MAXIMUM = 0x0100,
        COMPRESSION_ENGINE_HIBER = 0x0200
    }
}
