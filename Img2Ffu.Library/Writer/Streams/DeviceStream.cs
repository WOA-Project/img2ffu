/*

Copyright (c) 2019, Gustave Monce - gus33000.me - @gus33000
Copyright (c) 2018, Proto Beta Test - protobetatest.com - @ProtoBetaTest

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
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Img2Ffu.Streams
{
    public partial class DeviceStream : Stream
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;

        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_DEVICE = 0x40;
        private const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        private const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        private const uint DISK_BASE = 7;

        private const uint FILE_ANY_ACCESS = 0;
        private const uint FILE_SHARE_READ = 1;
        private const uint FILE_SHARE_WRITE = 2;

        private const uint FILE_DEVICE_FILE_SYSTEM = 9;
        private const uint METHOD_BUFFERED = 0;

        private static readonly uint DISK_GET_DRIVE_GEOMETRY_EX = CTL_CODE(DISK_BASE, 0x0028, METHOD_BUFFERED, FILE_ANY_ACCESS);
        private static readonly uint FSCTL_LOCK_VOLUME = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 6, METHOD_BUFFERED, FILE_ANY_ACCESS);
        private static readonly uint FSCTL_UNLOCK_VOLUME = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 7, METHOD_BUFFERED, FILE_ANY_ACCESS);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(nint hFile, byte[] lpBuffer, int nNumberOfBytesToRead, ref int lpNumberOfBytesRead, nint lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(nint hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, ref int lpNumberOfBytesWritten, nint lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, nint lpInBuffer, uint nInBufferSize, nint lpOutBuffer, int nOutBufferSize, ref uint lpBytesReturned, nint lpOverlapped);

        [DllImport("kernel32.dll")]
        private static extern bool SetFilePointerEx(SafeFileHandle hFile, long liDistanceToMove, out long lpNewFilePointer, uint dwMoveMethod);

        private SafeFileHandle? handleValue = null;
        private long _Position = 0;
        private readonly long _length = 0;
        private readonly uint _sectorsize = 0;
        private readonly bool _canWrite = false;
        private readonly bool _canRead = false;
        private bool disposed = false;

        private static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return (DeviceType << 16) | (Access << 14) | (Function << 2) | Method;
        }

        public DeviceStream(string device, FileAccess access)
        {
            if (string.IsNullOrEmpty(device))
            {
                throw new ArgumentNullException(nameof(device));
            }

            uint fileAccess = 0;
            switch (access)
            {
                case FileAccess.Read:
                    fileAccess = GENERIC_READ;
                    _canRead = true;
                    break;
                case FileAccess.ReadWrite:
                    fileAccess = GENERIC_READ | GENERIC_WRITE;
                    _canRead = true;
                    _canWrite = true;
                    break;
                case FileAccess.Write:
                    fileAccess = GENERIC_WRITE;
                    _canWrite = true;
                    break;
            }

            string devicePath = @"\\.\PhysicalDrive" + device.ToLower().Replace(@"\\.\physicaldrive", "");

            (_length, _sectorsize) = GetDiskProperties(devicePath);

            nint ptr = CreateFile(devicePath, fileAccess, 0, nint.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_DEVICE | FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH, nint.Zero);
            handleValue = new SafeFileHandle(ptr, true);

            if (handleValue.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            uint lpBytesReturned = 0;
            uint result = DeviceIoControl(handleValue, FSCTL_LOCK_VOLUME, nint.Zero, 0, nint.Zero, 0, ref lpBytesReturned, nint.Zero);

            if (result == 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        public override bool CanRead => _canRead;

        public override bool CanSeek => true;

        public override bool CanWrite => _canWrite;

        public override void Flush()
        {
            return;
        }

        public override long Length => _length;

        public override long Position
        {
            get => _Position;
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and 
        /// (offset + count - 1) replaced by the bytes read from the current source. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns></returns>
        private int InternalRead(byte[] buffer, int offset, int count)
        {
            int BytesRead = 0;
            byte[] BufBytes = new byte[count];
            if (!ReadFile(handleValue.DangerousGetHandle(), BufBytes, count, ref BytesRead, nint.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            for (int i = 0; i < BytesRead; i++)
            {
                buffer[offset + i] = BufBytes[i];
            }

            _Position += count;

            return BytesRead;
        }

        /// <summary>
        /// Some devices cannot read portions that are not modulo a sector, this aims to fix that issue.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and 
        /// (offset + count - 1) replaced by the bytes read from the current source. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count % _sectorsize != 0)
            {
                long extrastart = Position % _sectorsize;
                if (extrastart != 0)
                {
                    _ = Seek(-extrastart, SeekOrigin.Current);
                }

                long addedcount = _sectorsize - (count % _sectorsize);
                long ncount = count + addedcount;
                byte[] tmpbuffer = new byte[extrastart + buffer.Length + addedcount];
                buffer.CopyTo(tmpbuffer, extrastart);
                _ = InternalRead(tmpbuffer, offset + (int)extrastart, (int)ncount);
                tmpbuffer.ToList().Skip((int)extrastart).Take(count + offset).ToArray().CopyTo(buffer, 0);
                return count;
            }

            return InternalRead(buffer, offset, count);
        }

        public override int ReadByte()
        {
            int BytesRead = 0;
            byte[] lpBuffer = new byte[1];
            if (!ReadFile(
            handleValue.DangerousGetHandle(),                        // handle to file
            lpBuffer,                                                // data buffer
            1,                                                       // number of bytes to read
            ref BytesRead,                                           // number of bytes read
            nint.Zero
            ))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                ;
            }

            _Position += 1;

            return lpBuffer[0];
        }

        public override void WriteByte(byte Byte)
        {
            int BytesWritten = 0;
            byte[] lpBuffer = [Byte];
            if (!WriteFile(
            handleValue.DangerousGetHandle(),                        // handle to file
            lpBuffer,                                                // data buffer
            1,                                                       // number of bytes to write
            ref BytesWritten,                                        // number of bytes written
            nint.Zero
            ))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                ;
            }

            _Position += 1;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long off = offset;

            switch (origin)
            {
                case SeekOrigin.Current:
                    off += _Position;
                    break;
                case SeekOrigin.End:
                    off += _length;
                    break;
            }

            if (!SetFilePointerEx(handleValue, off, out long ret, 0))
            {
                return _Position;
            }

            _Position = ret;

            return ret;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int BytesWritten = 0;
            byte[] BufBytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                BufBytes[offset + i] = buffer[i];
            }

            if (!WriteFile(handleValue.DangerousGetHandle(), BufBytes, count, ref BytesWritten, nint.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            _Position += count;
        }

        public override void Close()
        {
            uint lpBytesReturned = 0;
            uint result = DeviceIoControl(handleValue, FSCTL_UNLOCK_VOLUME, nint.Zero, 0, nint.Zero, 0, ref lpBytesReturned, nint.Zero);

            if (0 == result)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            handleValue.Close();
            handleValue.Dispose();
            handleValue = null;
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
                    if (handleValue != null)
                    {
                        uint lpBytesReturned = 0;
                        uint result = DeviceIoControl(handleValue, FSCTL_UNLOCK_VOLUME, nint.Zero, 0, nint.Zero, 0, ref lpBytesReturned, nint.Zero);

                        if (0 == result)
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }

                        handleValue.Close();
                        handleValue.Dispose();
                        handleValue = null;
                    }
                }
                // Note disposing has been done.
                disposed = true;
            }
        }

        private static (long, uint) GetDiskProperties(string deviceName)
        {
            DISK_GEOMETRY_EX x = new();
            Execute(ref x, DISK_GET_DRIVE_GEOMETRY_EX, deviceName);
            return (x.DiskSize, x.Geometry.BytesPerSector);
        }

        private static void Execute<T>(ref T x, uint dwIoControlCode, string lpFileName, uint dwDesiredAccess = GENERIC_READ, uint dwShareMode = FILE_SHARE_WRITE | FILE_SHARE_READ, nint lpSecurityAttributes = default, uint dwCreationDisposition = OPEN_EXISTING, uint dwFlagsAndAttributes = 0, nint hTemplateFile = default)
        {
            nint hDevice = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

            SafeFileHandle handleValue = new(hDevice, true);

            if (handleValue.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            int nOutBufferSize = Marshal.SizeOf(typeof(T));
            nint lpOutBuffer = Marshal.AllocHGlobal(nOutBufferSize);
            uint lpBytesReturned = default;

            uint result = DeviceIoControl(handleValue, dwIoControlCode, nint.Zero, 0, lpOutBuffer, nOutBufferSize, ref lpBytesReturned, nint.Zero);

            if (result == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            x = (T)Marshal.PtrToStructure(lpOutBuffer, typeof(T));
            Marshal.FreeHGlobal(lpOutBuffer);

            handleValue.Close();
            handleValue.Dispose();
        }
    }
}