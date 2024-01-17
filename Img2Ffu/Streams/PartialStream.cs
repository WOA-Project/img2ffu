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
using System.IO;

namespace Img2Ffu.Streams
{
    internal class PartialStream : Stream
    {
        private Stream innerstream;

        private bool disposed;
        private readonly long start;
        private readonly long end;

        public PartialStream(Stream stream, long StartOffset, long EndOffset)
        {
            stream.Seek(StartOffset, SeekOrigin.Begin);
            start = StartOffset;
            end = EndOffset;
            innerstream = stream;
        }

        public override bool CanRead => innerstream.CanRead;
        public override bool CanSeek => innerstream.CanSeek;
        public override bool CanWrite => innerstream.CanWrite;
        public override long Length => end - start;
        public override long Position
        {
            get => innerstream.Position - start; set => innerstream.Position = value + start;
        }

        public override void Flush()
        {
            innerstream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerstream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                return innerstream.Seek(offset + start, origin);
            }

            if (origin == SeekOrigin.End)
            {
                return innerstream.Seek(end + offset, origin);
            }

            return innerstream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerstream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerstream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            innerstream.Dispose();
            innerstream = null;
            base.Close();
        }

        new void Dispose()
        {
            Dispose(true);
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        new void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                if (disposing)
                {
                    if (innerstream != null)
                    {
                        innerstream.Dispose();
                        innerstream = null;
                    }
                }

                // Note disposing has been done.
                disposed = true;
            }
        }
    }
}