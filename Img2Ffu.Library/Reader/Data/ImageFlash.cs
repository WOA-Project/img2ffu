using Img2Ffu.Reader.Compression;
using Img2Ffu.Reader.Enums;
using Img2Ffu.Reader.Structs;
using System.Text;

namespace Img2Ffu.Reader.Data
{
    internal class ImageFlash
    {
        public ImageHeader ImageHeader;
        public string ManifestData = "";
        public byte[] Padding = [];
        public readonly List<Store> Stores = [];

        private readonly long DataBlocksPosition;
        private readonly long InitialStreamPosition;


        public ImageFlash(Stream stream)
        {
            InitialStreamPosition = stream.Position;
            ImageHeader = stream.ReadStructure<ImageHeader>();

            if (ImageHeader.Signature != "ImageFlash  ")
            {
                throw new InvalidDataException("Invalid Image Header Signature!");
            }

            byte[] manifestDataBuffer = new byte[ImageHeader.ManifestLength];
            _ = stream.Read(manifestDataBuffer, 0, (int)ImageHeader.ManifestLength);
            ASCIIEncoding asciiEncoding = new();
            ManifestData = asciiEncoding.GetString(manifestDataBuffer);

            long Position = stream.Position;
            if (Position % (ImageHeader.ChunkSize * 1024) > 0)
            {
                long paddingSize = (ImageHeader.ChunkSize * 1024) - (Position % (ImageHeader.ChunkSize * 1024));
                Padding = new byte[paddingSize];
                _ = stream.Read(Padding, 0, (int)paddingSize);
            }

            Store store = new(stream);
            Stores.Add(store);

            if (store.StoreHeader.MajorVersion == 2 && store.StoreHeader.FullFlashMajorVersion == 2)
            {
                for (uint i = 2; i <= store.StoreHeaderV2.NumberOfStores; i++)
                {
                    Stores.Add(new Store(stream));
                }
            }

            DataBlocksPosition = stream.Position;
        }


        public ulong GetImageBlockCount()
        {
            ulong imageBlockCount = (ulong)(DataBlocksPosition - InitialStreamPosition) / (ImageHeader.ChunkSize * 1024);
            ulong storeBlockCount = GetDataBlockCount();
            return imageBlockCount + storeBlockCount;
        }

        public byte[] GetImageBlock(Stream Stream, ulong dataBlockIndex)
        {
            ulong imageBlockCount = (ulong)(DataBlocksPosition - InitialStreamPosition) / (ImageHeader.ChunkSize * 1024);

            // The data block is within the image headers
            if (dataBlockIndex < imageBlockCount)
            {
                byte[] dataBlock = new byte[(ImageHeader.ChunkSize * 1024)];
                ulong dataBlockPosition = (ulong)InitialStreamPosition + (dataBlockIndex * (ImageHeader.ChunkSize * 1024));

                _ = Stream.Seek((long)dataBlockPosition, SeekOrigin.Begin);
                _ = Stream.Read(dataBlock, 0, dataBlock.Length);

                return dataBlock;
            }
            // The data block is within the store data blocks, those may be compressed
            else
            {
                return GetDataBlock(Stream, dataBlockIndex - imageBlockCount);
            }
        }


        public ulong GetDataBlockCount()
        {
            ulong dataBlockCount = 0;
            foreach (Store store in Stores)
            {
                dataBlockCount += (ulong)store.WriteDescriptors.LongCount();
            }
            return dataBlockCount;
        }

        public byte[] GetDataBlock(Stream Stream, ulong dataBlockIndex)
        {
            ulong dataBlockIndexOffset = 0;
            for (ulong storeIndex = 0; storeIndex < (ulong)Stores.Count; storeIndex++)
            {
                ulong storeDataBlockCount = GetStoreDataBlockCount(storeIndex);
                if (dataBlockIndexOffset + storeDataBlockCount > dataBlockIndex)
                {
                    return GetStoreDataBlock(Stream, storeIndex, dataBlockIndex - dataBlockIndexOffset);
                }

                dataBlockIndexOffset += storeDataBlockCount;
            }

            throw new Exception("Invalid data block index value");
        }


        public ulong GetStoreDataBlockCount(ulong storeIndex)
        {
            return (ulong)Stores[(int)storeIndex].WriteDescriptors.LongCount();
        }

        public byte[] GetStoreDataBlock(Stream Stream, ulong storeIndex, ulong dataBlockIndex)
        {
            ulong dataBlockPosition = (ulong)DataBlocksPosition;
            for (ulong s = 0; s < storeIndex; s++)
            {
                Store store = Stores[(int)s];
                if (store.CompressionAlgorithm != 0)
                {
                    foreach (WriteDescriptor writeDescriptor in store.WriteDescriptors)
                    {
                        dataBlockPosition += writeDescriptor.DataSize;
                    }
                }
                else
                {
                    dataBlockPosition += store.StoreHeader.WriteDescriptorCount * store.StoreHeader.BlockSize;
                }
            }

            Store currentStore = Stores[(int)storeIndex];
            byte[] dataBlock = new byte[currentStore.StoreHeader.BlockSize];

            CompressionAlgorithm compressionAlgorithm = (CompressionAlgorithm)currentStore.CompressionAlgorithm;

            if (compressionAlgorithm != CompressionAlgorithm.None)
            {
                for (ulong i = 0; i < dataBlockIndex; i++)
                {
                    WriteDescriptor writeDescriptor = currentStore.WriteDescriptors[(int)i];
                    dataBlockPosition += writeDescriptor.DataSize;
                }

                WriteDescriptor currentWriteDescriptor = currentStore.WriteDescriptors[(int)dataBlockIndex];
                uint compressedDataBlockSize = currentWriteDescriptor.DataSize;
                byte[] compressedDataBlock = new byte[compressedDataBlockSize];

                _ = Stream.Seek((long)dataBlockPosition, SeekOrigin.Begin);
                _ = Stream.Read(compressedDataBlock, 0, compressedDataBlock.Length);

                switch ((CompressionAlgorithm)currentStore.CompressionAlgorithm)
                {
                    case CompressionAlgorithm.LZNT1:
                        {
                            dataBlock = WindowsNativeCompression.Decompress(WindowsNativeCompressionAlgorithm.COMPRESSION_FORMAT_LZNT1, compressedDataBlock, (int)currentStore.StoreHeader.BlockSize);
                            break;
                        }
                    case CompressionAlgorithm.Default:
                    case CompressionAlgorithm.XPRESS:
                        {
                            dataBlock = WindowsNativeCompression.Decompress(WindowsNativeCompressionAlgorithm.COMPRESSION_FORMAT_XPRESS, compressedDataBlock, (int)currentStore.StoreHeader.BlockSize);
                            break;
                        }
                    case CompressionAlgorithm.XPRESS_HUFF:
                        {
                            dataBlock = WindowsNativeCompression.Decompress(WindowsNativeCompressionAlgorithm.COMPRESSION_FORMAT_XPRESS_HUFF, compressedDataBlock, (int)currentStore.StoreHeader.BlockSize);
                            break;
                        }
                    default:
                        {
                            throw new NotImplementedException("The compression algorithm this data block uses is not currently implemented.");
                        }
                }
            }
            else
            {
                dataBlockPosition += dataBlockIndex * currentStore.StoreHeader.BlockSize;

                _ = Stream.Seek((long)dataBlockPosition, SeekOrigin.Begin);
                _ = Stream.Read(dataBlock, 0, dataBlock.Length);
            }

            return dataBlock;
        }
    }
}
