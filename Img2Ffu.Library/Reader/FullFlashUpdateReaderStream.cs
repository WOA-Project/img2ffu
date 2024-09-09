using Img2Ffu.Reader.Data;

namespace Img2Ffu.Reader
{
    public class FullFlashUpdateReaderStream : Stream
    {
        private readonly Stream ffuFileStream;
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
            ffuFileStream = File.OpenRead(FFUFilePath);
            signedImage = new SignedImage(ffuFileStream);
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
                    throw new ArgumentOutOfRangeException("value");
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
                throw new ArgumentNullException("buffer");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("The sum of offset and count is greater than the buffer length.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
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

            byte[] readBuffer = new byte[readBytes];
            Array.Fill<byte>(readBuffer, 0);

            // Read the buffer from the FFU file.
            // First we have to figure out where do we land here.

            long overflowBlockStartByteCount = Position % blockSize;
            long overflowBlockEndByteCount = (Position + readBytes) % blockSize;

            long startBlockIndex = (Position - overflowBlockStartByteCount) / blockSize;
            long endBlockIndex = (Position + readBytes + (blockSize - overflowBlockEndByteCount)) / blockSize;

            byte[] allReadBlocks = new byte[(endBlockIndex - startBlockIndex + 1) * blockSize];

            for (long currentBlock = startBlockIndex; currentBlock < endBlockIndex; currentBlock++)
            {
                long virtualBlockIndex = GetBlockDataIndex(currentBlock);
                if (virtualBlockIndex != -1)
                {
                    byte[] block = image.GetStoreDataBlock(ffuFileStream, storeIndex, (ulong)virtualBlockIndex);
                    Array.Copy(block, 0, allReadBlocks, (int)((currentBlock - startBlockIndex) * blockSize), blockSize);
                }
            }

            Array.Copy(allReadBlocks, overflowBlockStartByteCount, buffer, offset, readBytes);

            Position += readBytes;

            if (Position == Length)
            {
                // Workaround for malformed MBRs
                //return 0;
            }

            return (int)readBytes;
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
            ffuFileStream.Dispose();
        }
    }
}
