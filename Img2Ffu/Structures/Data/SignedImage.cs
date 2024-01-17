using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Img2Ffu.Structures.Structs;

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

        private readonly Stream Stream;
        private readonly long InitialStreamPosition;
        private readonly long ImageHeaderPosition;

        public SignedImage(Stream stream)
        {
            Stream = stream;
            InitialStreamPosition = stream.Position;

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
            if (Position % (SecurityHeader.DwChunkSizeInKb * 1024) > 0)
            {
                long paddingSize = SecurityHeader.DwChunkSizeInKb * 1024 - Position % (SecurityHeader.DwChunkSizeInKb * 1024);
                Padding = new byte[paddingSize];
                stream.Read(Padding, 0, (int)paddingSize);
            }

            ImageHeaderPosition = stream.Position;
            long signedAreaSize = Stream.Length - ImageHeaderPosition;
            uint sizeOfBlock = SecurityHeader.DwChunkSizeInKb * 1024;
            long numberOfBlocksToVerify = signedAreaSize / sizeOfBlock;
            long sizeOfHash = HashTable.LongLength / numberOfBlocksToVerify;

            for (int i = 0; i < HashTable.Length; i += (int)sizeOfHash)
            {
                BlockHashes.Add(HashTable[i..(i + (int)sizeOfHash)]);
            }

            Image = new ImageFlash(stream);
        }

        public void VerifyFFU()
        {
            long signedAreaSize = Stream.Length - ImageHeaderPosition;
            uint sizeOfBlock = SecurityHeader.DwChunkSizeInKb * 1024;
            long numberOfBlocksToVerify = signedAreaSize / sizeOfBlock;
            long sizeOfHash = HashTable.LongLength / numberOfBlocksToVerify;

            long oldPosition = Stream.Position;
            Stream.Seek(ImageHeaderPosition, SeekOrigin.Begin);

            using BinaryReader binaryReader = new(Stream, Encoding.ASCII, true);
            using BinaryReader hashTableBinaryReader = new(new MemoryStream(HashTable));

            for (long i = 0; i < numberOfBlocksToVerify; i++)
            {
                Console.Title = $"{i}/{numberOfBlocksToVerify}";

                if (SecurityHeader.DwAlgId == 0x0000800c) // SHA256
                {
                    byte[] hash = SHA256.HashData(binaryReader.ReadBytes((int)sizeOfBlock));
                    byte[] hashTableHash = hashTableBinaryReader.ReadBytes((int)sizeOfHash);

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