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
namespace Img2Ffu.Writer.Streams
{
    internal class PartialStream : Stream
    {
        private Stream? innerStream;

        private bool disposed;
        private readonly long startOffset;
        private readonly long endOffset;

        public PartialStream(Stream stream, long StartOffset, long EndOffset)
        {
            _ = stream.Seek(StartOffset, SeekOrigin.Begin);
            startOffset = StartOffset;
            endOffset = EndOffset;
            innerStream = stream;
        }

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => endOffset - startOffset;
        public override long Position
        {
            get => innerStream.Position - startOffset; set => innerStream.Position = value + startOffset;
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin == SeekOrigin.Begin
                ? innerStream.Seek(offset + startOffset, origin)
                : origin == SeekOrigin.End ? innerStream.Seek(endOffset + offset, origin) : innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            innerStream.Dispose();
            innerStream = null;
            base.Close();
        }

        private new void Dispose()
        {
            Dispose(true);
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        private new void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                if (disposing)
                {
                    if (innerStream != null)
                    {
                        innerStream.Dispose();
                        innerStream = null;
                    }
                }

                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}