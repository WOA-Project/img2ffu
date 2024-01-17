using System.Collections.Generic;
using System.IO;
using System.Text;
using Img2Ffu.Structures.Structs;

namespace Img2Ffu.Structures.Data
{
    internal class ImageFlash
    {
        public ImageHeader ImageHeader;
        public string ManifestData = "";
        public byte[] Padding = [];
        public readonly List<Store> Stores = [];
        private readonly long DataBlocksPosition;

        private readonly Stream Stream;
        private readonly long InitialStreamPosition;

        public ImageFlash(Stream stream)
        {
            Stream = stream;
            InitialStreamPosition = stream.Position;

            ImageHeader = stream.ReadStructure<ImageHeader>();

            if (ImageHeader.Signature != "ImageFlash  ")
            {
                throw new InvalidDataException("Invalid Image Header Signature!");
            }

            byte[] manifestDataBuffer = new byte[ImageHeader.ManifestLength];
            stream.Read(manifestDataBuffer, 0, (int)ImageHeader.ManifestLength);
            ASCIIEncoding asciiEncoding = new();
            ManifestData = asciiEncoding.GetString(manifestDataBuffer);

            long Position = stream.Position;
            if (Position % (ImageHeader.DwChunkSize * 1024) > 0)
            {
                long paddingSize = ImageHeader.DwChunkSize * 1024 - Position % (ImageHeader.DwChunkSize * 1024);
                Padding = new byte[paddingSize];
                stream.Read(Padding, 0, (int)paddingSize);
            }

            Store store = new(stream);
            Stores.Add(store);

            if (store.StoreHeader.MajorVersion == 2 && store.StoreHeader.FullFlashMajorVersion == 2)
            {
                for (uint i = 2; i <= store.StoreHeaderV2.NumOfStores; i++)
                {
                    Stores.Add(new Store(stream));
                }
            }
            DataBlocksPosition = stream.Position;
        }

        public byte[] GetDataBlock(ulong dataBlockIndex)
        {
            ulong dataBlockPosition = (ulong)DataBlocksPosition + dataBlockIndex * ImageHeader.DwChunkSize * 1024;
            Stream.Seek((long)dataBlockPosition, SeekOrigin.Begin);
            byte[] dataBlock = new byte[ImageHeader.DwChunkSize * 1024];
            Stream.Read(dataBlock, 0, dataBlock.Length);
            return dataBlock;
        }

        public ulong GetDataBlockCount()
        {
            ulong dataBlockSectionLength = (ulong)Stream.Length - (ulong)DataBlocksPosition;
            return dataBlockSectionLength / (ImageHeader.DwChunkSize * 1024);
        }

        public ulong GetDataBlockCountForStore(ulong storeIndex)
        {
            ulong dataBlockSectionLength = Stores[(int)storeIndex].StoreHeaderV2.StorePayloadSize;
            return dataBlockSectionLength / (ImageHeader.DwChunkSize * 1024);
        }

        public byte[] GetDataBlock(ulong storeIndex, ulong dataBlockIndex)
        {
            ulong previousBlocks = 0;
            for (uint s = 0; s < storeIndex; s++)
            {
                previousBlocks += GetDataBlockCountForStore(s);
            }
            return GetDataBlock(previousBlocks + dataBlockIndex);
        }
    }
}
