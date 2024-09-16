using Img2Ffu.Reader.Data;
using Img2Ffu.Reader.Structs;

namespace Img2Ffu.Reader
{
    public class FullFlashUpdateReaderStream : Stream
    {
        private readonly Stream Stream;
        private readonly SignedImage signedImage;
        private readonly ImageFlash image;
        private readonly Store store;
        private readonly ulong storeIndex;
        private readonly long length;
        private readonly long blockSize;
        private readonly Dictionary<long, int> blockTable;

        private long currentPosition = 0;

        public int SectorSize
        {
            get;
        }
        public int MinSectorCount
        {
            get;
        }

        public string DevicePath => store.DevicePath;

        public FullFlashUpdateReaderStream(string FFUFilePath, ulong storeIndex)
        {
            Stream = File.OpenRead(FFUFilePath);
            signedImage = new SignedImage(Stream);
            image = signedImage.Image;
            this.storeIndex = storeIndex;
            store = image.Stores[(int)storeIndex];
            blockSize = store.StoreHeader.BlockSize;
            (length, blockTable) = BuildBlockTable();

            try
            {
                (int minSectorCount, int sectorSize)[] manifestStoreInformation = ExtractImageManifestStoreInformation(signedImage);

                (MinSectorCount, SectorSize) = manifestStoreInformation[storeIndex];

                if (SectorSize == 0)
                {
                    SectorSize = signedImage.ChunkSize;
                }

                length = (long)((ulong)MinSectorCount * (ulong)SectorSize);
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
                        currentMinSectorCount = success ? tempCurrentMinSectorCount : throw new InvalidDataException(line);
                    }
                    else if (line.StartsWith("SectorSize", StringComparison.InvariantCultureIgnoreCase))
                    {
                        bool success = int.TryParse(line.Split("=")[1].Trim(), out int tempCurrentMinSectorSize);
                        currentSectorSize = success ? tempCurrentMinSectorSize : throw new InvalidDataException(line);
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

        private (long, Dictionary<long, int>) BuildBlockTable()
        {
            // We do not want to immediately return because FFU files may first blank out sectors and then write post mortem
            Dictionary<long, int> blockTable = [];

            long blockMaxStart = 0;
            long blockMaxEnd = 0;

            for (int i = 0; i < store.WriteDescriptors.Count; i++)
            {
                WriteDescriptor writeDescriptor = store.WriteDescriptors[i];
                foreach (DiskLocation diskLocation in writeDescriptor.DiskLocations)
                {
                    switch (diskLocation.DiskAccessMethod)
                    {
                        case 0:
                            {
                                if (diskLocation.BlockIndex > blockMaxStart)
                                {
                                    blockMaxStart = diskLocation.BlockIndex;
                                }

                                long blockOffset = diskLocation.BlockIndex;
                                if (blockTable.ContainsKey(blockOffset))
                                {
                                    blockTable[blockOffset] = i;
                                }
                                else
                                {
                                    blockTable.Add(blockOffset, i);
                                }
                                break;
                            }
                        case 2:
                            {
                                if (diskLocation.BlockIndex > blockMaxEnd)
                                {
                                    blockMaxEnd = diskLocation.BlockIndex;
                                }

                                long blockOffset = (Length / blockSize) - 1 - diskLocation.BlockIndex;
                                if (blockTable.ContainsKey(blockOffset))
                                {
                                    blockTable[blockOffset] = i;
                                }
                                else
                                {
                                    blockTable.Add(blockOffset, i);
                                }
                                break;
                            }
                    }
                }
            }

            long totalBlocks = blockMaxStart + blockMaxEnd + 2;

            return (totalBlocks * blockSize, blockTable);
        }

        private long GetBlockDataIndex(long realBlockOffset)
        {
            if (blockTable.ContainsKey(realBlockOffset))
            {
                return blockTable[realBlockOffset];
            }

            return -1; // Invalid
        }

        public override long Position
        {
            get => currentPosition;
            set
            {
                if (currentPosition < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                // Workaround for malformed MBRs
                /*if (currentPosition > Length)
                {
                    throw new EndOfStreamException();
                }*/

                currentPosition = value;
            }
        }

        public override void Flush()
        {
            // Nothing to do here
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // Workaround for malformed MBRs
            if (Position >= Length)
            {
                return count;
            }

            long readBytes = count;

            if (Position + readBytes > Length)
            {
                readBytes = (int)(Length - Position);
            }

            // The number of bytes that do not line up with the size of blocks (blockSize) at the beginning
            long overflowBlockStartByteCount = Position % blockSize;

            // The number of bytes that do not line up with the size of blocks (blockSize) at the end
            long overflowBlockEndByteCount = (Position + readBytes) % blockSize;

            // The position to start reading from, aligned to the size of blocks (blockSize)
            long noOverflowBlockStartByteCount = Position - overflowBlockStartByteCount;

            // The number of extra bytes to read at the start
            long extraStartBytes = blockSize - overflowBlockStartByteCount;

            // The number of extra bytes to read at the end
            long extraEndBytes = blockSize - overflowBlockEndByteCount;

            // The position to end reading from, aligned to the size of blocks (blockSize) (excluding)
            long noOverflowBlockEndByteCount = Position + readBytes + extraEndBytes;

            // The first block we have to read
            long startBlockIndex = noOverflowBlockStartByteCount / blockSize;

            // The last block we have to read (excluding)
            long endBlockIndex = noOverflowBlockEndByteCount / blockSize;

            // Go through every block one by one
            for (long currentBlock = startBlockIndex; currentBlock < endBlockIndex; currentBlock++)
            {
                bool isFirstBlock = currentBlock == startBlockIndex;
                bool isLastBlock = currentBlock == endBlockIndex - 1;

                long bytesToRead = blockSize;
                long bufferDestination = extraStartBytes + (currentBlock - startBlockIndex - 1) * blockSize;

                if (isFirstBlock)
                {
                    bytesToRead = extraStartBytes;
                    bufferDestination = 0;
                }

                if (isLastBlock)
                {
                    bytesToRead -= extraEndBytes;
                }

                long virtualBlockIndex = GetBlockDataIndex(currentBlock);

                if (virtualBlockIndex != -1)
                {
                    // The block exists
                    byte[] block = image.GetStoreDataBlock(Stream, storeIndex, (ulong)virtualBlockIndex);
                    long physicalDiskLocation = 0;

                    if (isFirstBlock)
                    {
                        physicalDiskLocation += overflowBlockStartByteCount;
                    }

                    Array.Copy(block, physicalDiskLocation, buffer, offset + (int)bufferDestination, (int)bytesToRead);
                }
                else
                {
                    // The block does not exist in the pool, fill the area with 00s instead
                    Array.Fill<byte>(buffer, 0, offset + (int)bufferDestination, (int)bytesToRead);
                }
            }

            Position += readBytes;

            if (Position == Length)
            {
                // Workaround for malformed MBRs
                //return 0;
            }

            return (int)readBytes;
        }

        public void CopyTo(Stream DestinationStream, Action<ulong, ulong> ProgressCallBack)
        {
            long OriginalDestinationStreamPosition = DestinationStream.Position;

            ulong totalBytes = 0;

            for (int i = 0; i < store.WriteDescriptors.Count; i++)
            {
                WriteDescriptor writeDescriptor = store.WriteDescriptors[i];
                totalBytes += (ulong)writeDescriptor.DiskLocations.Count;
            }

            totalBytes *= (ulong)blockSize * 2u;

            ProgressCallBack?.Invoke(0, totalBytes);

            ulong currentWrittenBytes = 0;

            for (int i = 0; i < store.WriteDescriptors.Count; i++)
            {
                WriteDescriptor writeDescriptor = store.WriteDescriptors[i];
                foreach (DiskLocation slabAllocation in writeDescriptor.DiskLocations)
                {
                    long virtualDiskBlockNumber = long.MinValue;
                    long physicalDiskBlockNumber = i;

                    switch (slabAllocation.DiskAccessMethod)
                    {
                        case 0:
                            {
                                virtualDiskBlockNumber = slabAllocation.BlockIndex;
                                break;
                            }
                        case 2:
                            {
                                virtualDiskBlockNumber = (Length / blockSize) - 1 - slabAllocation.BlockIndex;
                                break;
                            }
                    }

                    long virtualPosition = virtualDiskBlockNumber * blockSize;

                    DestinationStream.Seek(OriginalDestinationStreamPosition + virtualPosition, SeekOrigin.Begin);

                    byte[] buffer = image.GetStoreDataBlock(Stream, storeIndex, (ulong)physicalDiskBlockNumber);
                    currentWrittenBytes += (ulong)blockSize;
                    ProgressCallBack?.Invoke(currentWrittenBytes, totalBytes);

                    DestinationStream.Write(buffer);
                    currentWrittenBytes += (ulong)blockSize;
                    ProgressCallBack?.Invoke(currentWrittenBytes, totalBytes);
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        Position = offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        Position += offset;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        Position = Length + offset;
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
            Stream.Dispose();
        }
    }
}
