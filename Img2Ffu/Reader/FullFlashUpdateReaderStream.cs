using Img2Ffu.Reader.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace Img2Ffu.Reader
{
    public class FullFlashUpdateReaderStream : Stream
    {
        private Stream ffuFileStream;
        private SignedImage signedImage;
        private ImageFlash image;
        private Store store;
        private ulong storeIndex;
        private long length;
        private int sectorSize;
        private int minSectorCount;

        private long currentPosition = 0;

        public int SectorSize => sectorSize;
        public int MinSectorCount => minSectorCount;

        public string DevicePath => store.DevicePath;

        public FullFlashUpdateReaderStream(string FFUFilePath, ulong storeIndex)
        {
            ffuFileStream = File.OpenRead(FFUFilePath);
            signedImage = new SignedImage(ffuFileStream);
            image = signedImage.Image;
            this.storeIndex = storeIndex;
            store = image.Stores[(int)storeIndex];
            length = GetStoreLength();

            try
            {
                (int minSectorCount, int sectorSize)[] manifestStoreInformation = ExtractImageManifestStoreInformation(signedImage);

                (minSectorCount, sectorSize) = manifestStoreInformation[storeIndex];

                if (sectorSize == 0)
                {
                    sectorSize = signedImage.ChunkSize;
                }

                length = (long)((ulong)minSectorCount * (ulong)sectorSize);
            }
            catch { }
        }

        public static int GetStoreCount(string FFUFilePath)
        {
            using FileStream ffuStream = File.Open(FFUFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            SignedImage ffuFile = new(ffuStream);
            return ffuFile.Image.Stores.Count;
        }

        private static (int minSectorCount, int minSectorSize)[] ExtractImageManifestStoreInformation(SignedImage ffuFile)
        {
            List<(int minSectorCount, int minSectorSize)> manifestStoreInformation = [];

            int currentMinSectorCount = 0;
            int currentSectorSize = 0;
            bool isInStoreSection = false;

            foreach (string line in ffuFile.Image.ManifestData.Split("\n"))
            {
                if (line.StartsWith('[') && line.Contains(']'))
                {
                    string sectionName = line.Split('[')[1].Split(']')[0];

                    if (isInStoreSection)
                    {
                        manifestStoreInformation.Add((currentMinSectorCount, currentSectorSize));
                    }

                    isInStoreSection = sectionName.Equals("Store", StringComparison.InvariantCultureIgnoreCase);
                }
                else if (isInStoreSection)
                {
                    if (line.StartsWith("MinSectorCount", StringComparison.InvariantCultureIgnoreCase))
                    {
                        bool success = int.TryParse(line.Split("=")[1].Trim(), out int tempCurrentMinSectorCount);
                        if (success)
                        {
                            currentMinSectorCount = tempCurrentMinSectorCount;
                        }
                        else
                        {
                            throw new InvalidDataException(line);
                        }
                    }
                    else if (line.StartsWith("SectorSize", StringComparison.InvariantCultureIgnoreCase))
                    {
                        bool success = int.TryParse(line.Split("=")[1].Trim(), out int tempCurrentMinSectorSize);
                        if (success)
                        {
                            currentSectorSize = tempCurrentMinSectorSize;
                        }
                        else
                        {
                            throw new InvalidDataException(line);
                        }
                    }
                }
            }

            if (isInStoreSection)
            {
                manifestStoreInformation.Add((currentMinSectorCount, currentSectorSize));
            }

            return [.. manifestStoreInformation];
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        private long GetStoreLength()
        {
            long blockMaxStart = 0;
            long blockMaxEnd = 0;

            foreach (WriteDescriptor writeDescriptor in store.WriteDescriptors)
            {
                foreach (Structs.DiskLocation diskLocation in writeDescriptor.DiskLocations)
                {
                    switch (diskLocation.DiskAccessMethod)
                    {
                        case 0:
                            {
                                if (diskLocation.BlockIndex > blockMaxStart)
                                {
                                    blockMaxStart = diskLocation.BlockIndex;
                                }
                                break;
                            }
                        case 2:
                            {
                                if (diskLocation.BlockIndex > blockMaxEnd)
                                {
                                    blockMaxEnd = diskLocation.BlockIndex;
                                }
                                break;
                            }
                    }
                }
            }

            long totalBlocks = blockMaxStart + blockMaxEnd + 2;

            return totalBlocks * store.StoreHeader.BlockSize;
        }

        private long GetBlockDataIndex(long realBlockOffset)
        {
            // We do not want to immediately return because FFU files may first blank out sectors and then write post mortem
            long matchedIndex = -1; // Invalid

            for (int i = 0; i < store.WriteDescriptors.Count; i++)
            {
                WriteDescriptor writeDescriptor = store.WriteDescriptors[i];
                foreach (Structs.DiskLocation diskLocation in writeDescriptor.DiskLocations)
                {
                    switch (diskLocation.DiskAccessMethod)
                    {
                        case 0:
                            {
                                long blockOffset = diskLocation.BlockIndex;
                                if (blockOffset == realBlockOffset)
                                {
                                    matchedIndex = i;
                                }
                                break;
                            }
                        case 2:
                            {
                                long blockOffset = (Length / store.StoreHeader.BlockSize) - 1 - diskLocation.BlockIndex;
                                if (blockOffset == realBlockOffset)
                                {
                                    matchedIndex = i;
                                }
                                break;
                            }
                    }
                }
            }

            return matchedIndex;
        }

        public override long Position
        {
            get => currentPosition;
            set => currentPosition = value;
        }

        public override void Flush()
        {
            // Nothing to do here
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readBytes = count;

            if (Position + readBytes > Length)
            {
                readBytes = (int)(Length - Position);
            }

            byte[] readBuffer = new byte[readBytes];
            Array.Fill<byte>(readBuffer, 0);

            // Read the buffer from the FFU file.
            // First we have to figure out where do we land here.

            long overflowBlockStartByteCount = Position % store.StoreHeader.BlockSize;
            long overflowBlockEndByteCount = (Position + readBytes) % store.StoreHeader.BlockSize;

            long totalBlockCount = Length / store.StoreHeader.BlockSize;

            long startBlockIndex = (Position - overflowBlockStartByteCount) / store.StoreHeader.BlockSize;
            long endBlockIndex = (Position + readBytes + (store.StoreHeader.BlockSize - overflowBlockEndByteCount)) / store.StoreHeader.BlockSize;

            byte[] allReadBlocks = new byte[(endBlockIndex - startBlockIndex + 1) * store.StoreHeader.BlockSize];

            for (long currentBlock = startBlockIndex; currentBlock < endBlockIndex; currentBlock++)
            {
                long virtualBlockIndex = GetBlockDataIndex(currentBlock);
                if (virtualBlockIndex != -1)
                {
                    byte[] block = image.GetStoreDataBlock(ffuFileStream, storeIndex, (ulong)virtualBlockIndex);
                    Array.Copy(block, 0, allReadBlocks, (int)((currentBlock - startBlockIndex) * store.StoreHeader.BlockSize), store.StoreHeader.BlockSize);
                }
            }

            Array.Copy(allReadBlocks, overflowBlockStartByteCount, buffer, offset, readBytes);

            Position += readBytes;
            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        if (offset < 0)
                        {
                            throw new IOException();
                        }

                        Position = offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        int tempPosition = (int)Position + (int)offset;
                        if (tempPosition < 0)
                        {
                            throw new IOException();
                        }

                        Position = tempPosition;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        int tempPosition = (int)Length + (int)offset;
                        if (tempPosition < 0)
                        {
                            throw new IOException();
                        }

                        Position = tempPosition;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException(nameof(origin));
                    }
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ffuFileStream.Dispose();
        }
    }
}
