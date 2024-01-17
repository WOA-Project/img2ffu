using Img2Ffu.Structures.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Img2Ffu.Structures.Data
{
    internal class SignedImage
    {
        public SecurityHeader SecurityHeader;
        public byte[] SignedCatalog;
        public byte[] HashTable;
        public byte[] Padding = [];
        public ImageFlash Image;

        public readonly List<byte[]> BlockHashes = [];
        public X509Certificate Certificate;

        private readonly Stream Stream;

        public SignedImage(Stream stream)
        {
            Stream = stream;

            SecurityHeader = stream.ReadStructure<SecurityHeader>();

            if (SecurityHeader.Signature != "SignedImage ")
            {
                throw new InvalidDataException("Invalid Security Header Signature!");
            }

            SignedCatalog = new byte[SecurityHeader.DwCatalogSize];
            stream.Read(SignedCatalog, 0, (int)SecurityHeader.DwCatalogSize);

            HashTable = new byte[SecurityHeader.DwHashTableSize];
            stream.Read(HashTable, 0, (int)SecurityHeader.DwHashTableSize);

            long Position = stream.Position;
            uint sizeOfBlock = SecurityHeader.DwChunkSizeInKb * 1024;
            if (Position % sizeOfBlock > 0)
            {
                long paddingSize = sizeOfBlock - Position % sizeOfBlock;
                Padding = new byte[paddingSize];
                stream.Read(Padding, 0, (int)paddingSize);
            }

            ParseBlockHashes();

            try
            {
                Certificate = new X509Certificate(SignedCatalog);
            }
            catch { }

            Image = new ImageFlash(stream);
        }

        private void ParseBlockHashes()
        {
            BlockHashes.Clear();

            uint sizeOfBlock = SecurityHeader.DwChunkSizeInKb * 1024;
            uint ImageHeaderPosition = GetImageHeaderPosition();

            long signedAreaSize = Stream.Length - ImageHeaderPosition;
            long numberOfBlocksToVerify = signedAreaSize / sizeOfBlock;
            long sizeOfHash = HashTable.LongLength / numberOfBlocksToVerify;

            for (int i = 0; i < HashTable.Length; i += (int)sizeOfHash)
            {
                BlockHashes.Add(HashTable[i..(i + (int)sizeOfHash)]);
            }
        }

        private uint GetImageHeaderPosition()
        {
            uint sizeOfBlock = SecurityHeader.DwChunkSizeInKb * 1024;
            uint ImageHeaderPosition = SecurityHeader.CbSize + SecurityHeader.DwCatalogSize + SecurityHeader.DwHashTableSize;
            if (ImageHeaderPosition % sizeOfBlock > 0)
            {
                uint paddingSize = sizeOfBlock - ImageHeaderPosition % sizeOfBlock;
                ImageHeaderPosition += paddingSize;
            }
            return ImageHeaderPosition;
        }

        public void VerifyFFU()
        {
            uint sizeOfBlock = SecurityHeader.DwChunkSizeInKb * 1024;
            uint ImageHeaderPosition = GetImageHeaderPosition();

            long signedAreaSize = Stream.Length - ImageHeaderPosition;
            long numberOfBlocksToVerify = signedAreaSize / sizeOfBlock;

            long oldPosition = Stream.Position;
            Stream.Seek(ImageHeaderPosition, SeekOrigin.Begin);

            using BinaryReader binaryReader = new(Stream, Encoding.ASCII, true);
            using BinaryReader hashTableBinaryReader = new(new MemoryStream(HashTable));

            for (long i = 0; i < numberOfBlocksToVerify; i++)
            {
                Console.Title = $"{i}/{numberOfBlocksToVerify}";

                if (SecurityHeader.DwAlgId == 0x0000800c) // SHA256 Algorithm ID
                {
                    byte[] hash = SHA256.HashData(binaryReader.ReadBytes((int)sizeOfBlock));
                    byte[] hashTableHash = BlockHashes.ElementAt((int)i);

                    if (!StructuralComparisons.StructuralEqualityComparer.Equals(hash, hashTableHash))
                    {
                        Stream.Seek(oldPosition, SeekOrigin.Begin);
                        throw new InvalidDataException($"The FFU image contains a mismatched hash value at chunk {i}. " +
                            $"Expected: {BitConverter.ToString(hashTableHash).Replace("-", "")} " +
                            $"Computed: {BitConverter.ToString(hash).Replace("-", "")}");
                    }
                }
                else
                {
                    throw new InvalidDataException($"Unknown Hash algorithm id: {SecurityHeader.DwAlgId}");
                }
            }

            Console.Title = $"{numberOfBlocksToVerify}/{numberOfBlocksToVerify}";

            Stream.Seek(oldPosition, SeekOrigin.Begin);
        }
    }
}