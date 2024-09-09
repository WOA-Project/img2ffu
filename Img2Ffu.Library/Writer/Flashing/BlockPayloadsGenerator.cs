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
using Img2Ffu.Writer.Data;
using System.Collections;
using System.Security.Cryptography;

namespace Img2Ffu.Writer.Flashing
{
    internal class BlockPayloadsGenerator
    {
        private static void ShowProgress(ulong CurrentProgress, ulong TotalProgress, DateTime startTime, bool DisplayRed, ILogging Logging)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            double milliseconds = timeSoFar.TotalMilliseconds / CurrentProgress * (TotalProgress - CurrentProgress);
            double ticks = milliseconds * TimeSpan.TicksPerMillisecond;
            if (ticks > long.MaxValue || ticks < long.MinValue || double.IsNaN(ticks))
            {
                milliseconds = 0;
            }
            TimeSpan remaining = TimeSpan.FromMilliseconds(milliseconds);

            Logging.Log(string.Format($"{LoggingHelpers.GetDismLikeProgBar(int.Parse((CurrentProgress * 100 / TotalProgress).ToString()))} {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}"), returnLine: false, severity: DisplayRed ? ILoggingLevel.Warning : ILoggingLevel.Information);
        }

        internal static KeyValuePair<ByteArrayKey, BlockPayload>[] GetGPTPayloads(KeyValuePair<ByteArrayKey, BlockPayload>[] blockPayloads, Stream stream, uint BlockSize, bool IsFixedDiskLength)
        {
            List<KeyValuePair<ByteArrayKey, BlockPayload>> blockPayloadsList = [.. blockPayloads];

            byte[] EMPTY_BLOCK_HASH = SHA256.HashData(new byte[BlockSize]);

            // Erase both GPTs as per spec first
            blockPayloadsList.Insert(0, new KeyValuePair<ByteArrayKey, BlockPayload>(new ByteArrayKey(EMPTY_BLOCK_HASH), new BlockPayload(
                new WriteDescriptor()
                {
                    BlockDataEntry = new BlockDataEntry()
                    {
                        BlockCount = 1,
                        LocationCount = 2
                    },
                    DiskLocations =
                    [
                        new DiskLocation()
                        {
                            BlockIndex = 0,
                            DiskAccessMethod = 0
                        },
                        new DiskLocation()
                        {
                            BlockIndex = 0,
                            DiskAccessMethod = 2 // From End
                        }
                    ]
                },
                new MemoryStream(new byte[(int)BlockSize]),
                0
            )));

            byte[] PrimaryGPTBuffer = new byte[(int)BlockSize];
            _ = stream.Seek(0, SeekOrigin.Begin);
            _ = stream.Read(PrimaryGPTBuffer, 0, (int)BlockSize);

            MemoryStream primaryGPTStream = new(PrimaryGPTBuffer);

            KeyValuePair<ByteArrayKey, BlockPayload> primaryGPTKeyValuePair = new(new ByteArrayKey(SHA256.HashData(primaryGPTStream)), new BlockPayload(
                new WriteDescriptor()
                {
                    BlockDataEntry = new BlockDataEntry()
                    {
                        BlockCount = 1,
                        LocationCount = 1
                    },
                    DiskLocations =
                    [
                        new DiskLocation()
                        {
                            BlockIndex = 0,
                            DiskAccessMethod = 0
                        }
                    ]
                },
                primaryGPTStream,
                0
            ));

            if (IsFixedDiskLength)
            {
                ulong endGPTChunkStartLocation = (ulong)stream.Length - BlockSize;
                byte[] SecondaryGPTBuffer = new byte[(int)BlockSize];
                _ = stream.Seek((long)endGPTChunkStartLocation, SeekOrigin.Begin);
                _ = stream.Read(SecondaryGPTBuffer, 0, (int)BlockSize);

                MemoryStream secondaryGPTStream = new(SecondaryGPTBuffer);

                // Now add back both GPTs
                // Apparently the first one needs to be added twice?
                blockPayloadsList.Add(primaryGPTKeyValuePair);
                blockPayloadsList.Add(primaryGPTKeyValuePair);

                blockPayloadsList.Add(new KeyValuePair<ByteArrayKey, BlockPayload>(new ByteArrayKey(SHA256.HashData(secondaryGPTStream)), new BlockPayload(
                    new WriteDescriptor()
                    {
                        BlockDataEntry = new BlockDataEntry()
                        {
                            BlockCount = 1,
                            LocationCount = 1
                        },
                        DiskLocations =
                        [
                            new DiskLocation()
                            {
                                BlockIndex = 0,
                                DiskAccessMethod = 2
                            }
                        ]
                    },
                    secondaryGPTStream,
                    0
                )));
            }
            else
            {
                // Now add back both GPTs
                // Apparently the first one needs to be added twice?
                blockPayloadsList.Insert(1, primaryGPTKeyValuePair);
                blockPayloadsList.Add(primaryGPTKeyValuePair);

                ulong endGPTChunkStartLocation = (ulong)stream.Length - BlockSize;
                byte[] SecondaryGPTBuffer = new byte[(int)BlockSize];
                _ = stream.Seek((long)endGPTChunkStartLocation, SeekOrigin.Begin);
                _ = stream.Read(SecondaryGPTBuffer, 0, (int)BlockSize);

                MemoryStream secondaryGPTStream = new(SecondaryGPTBuffer);

                // HACK
                blockPayloadsList.Add(new KeyValuePair<ByteArrayKey, BlockPayload>(new ByteArrayKey(SHA256.HashData(secondaryGPTStream)), new BlockPayload(
                    new WriteDescriptor()
                    {
                        BlockDataEntry = new BlockDataEntry()
                        {
                            BlockCount = 1,
                            LocationCount = 1
                        },
                        DiskLocations =
                        [
                            new DiskLocation()
                            {
                                BlockIndex = 0,
                                DiskAccessMethod = 2
                            }
                        ]
                    },
                    secondaryGPTStream,
                    0
                )));
            }

            return [.. blockPayloadsList];
        }

        internal static KeyValuePair<ByteArrayKey, BlockPayload>[] GetOptimizedPayloads(FlashPart[] flashParts, uint BlockSize, uint BlankSectorBufferSize, ILogging Logging)
        {
            List<KeyValuePair<ByteArrayKey, BlockPayload>> hashedBlocks = [];

            if (flashParts == null)
            {
                return [.. hashedBlocks];
            }

            ulong CurrentBlockCount = 0;
            ulong TotalBlockCount = 0;
            foreach (FlashPart flashPart in flashParts)
            {
                TotalBlockCount += (ulong)flashPart.Stream.Length / BlockSize;
            }

            DateTime startTime = DateTime.Now;

            Logging.Log($"Total Block Count: {TotalBlockCount} - {TotalBlockCount * BlockSize / (1024 * 1024 * 1024)}GB");
            Logging.Log("Hashing resources...");

            bool blankPayloadPhase = false;
            ulong blankPayloadCount = 0;

            byte[] EMPTY_BLOCK_HASH = SHA256.HashData(new byte[BlockSize]);

            List<KeyValuePair<ByteArrayKey, BlockPayload>> blankBlocks = [];

            foreach (FlashPart flashPart in flashParts)
            {
                _ = flashPart.Stream.Seek(0, SeekOrigin.Begin);
                long totalBlockCount = flashPart.Stream.Length / BlockSize;

                for (uint blockIndex = 0; blockIndex < totalBlockCount; blockIndex++)
                {
                    byte[] blockBuffer = new byte[BlockSize];
                    long streamPosition = flashPart.Stream.Position;
                    _ = flashPart.Stream.Read(blockBuffer, 0, (int)BlockSize);
                    byte[] blockHash = SHA256.HashData(blockBuffer);

                    if (!StructuralComparisons.StructuralEqualityComparer.Equals(EMPTY_BLOCK_HASH, blockHash))
                    {
                        hashedBlocks.Add(new KeyValuePair<ByteArrayKey, BlockPayload>(new ByteArrayKey(blockHash), new BlockPayload(
                            new WriteDescriptor()
                            {
                                BlockDataEntry = new BlockDataEntry()
                                {
                                    BlockCount = 1,
                                    LocationCount = 1
                                },
                                DiskLocations =
                                [
                                    new DiskLocation()
                                    {
                                        BlockIndex = (uint)((flashPart.StartLocation / BlockSize) + blockIndex),
                                        DiskAccessMethod = 0
                                    }
                                ]
                            },
                            flashPart.Stream,
                            (ulong)streamPosition
                        )));

                        if (blankPayloadPhase && blankPayloadCount < BlankSectorBufferSize)
                        {
                            foreach (KeyValuePair<ByteArrayKey, BlockPayload> blankPayload in blankBlocks)
                            {
                                hashedBlocks.Add(blankPayload);
                            }
                        }

                        blankPayloadPhase = false;
                        blankPayloadCount = 0;
                        blankBlocks.Clear();
                    }
                    else if (blankPayloadCount < BlankSectorBufferSize)
                    {
                        blankPayloadPhase = true;
                        blankPayloadCount++;

                        blankBlocks.Add(new KeyValuePair<ByteArrayKey, BlockPayload>(new ByteArrayKey(blockHash), new BlockPayload(
                            new WriteDescriptor()
                            {
                                BlockDataEntry = new BlockDataEntry()
                                {
                                    BlockCount = 1,
                                    LocationCount = 1
                                },
                                DiskLocations =
                                [
                                    new DiskLocation()
                                    {
                                        BlockIndex = (uint)((flashPart.StartLocation / BlockSize) + blockIndex),
                                        DiskAccessMethod = 0
                                    }
                                ]
                            },
                            flashPart.Stream,
                            (ulong)streamPosition
                        )));
                    }
                    else if (blankPayloadCount >= BlankSectorBufferSize && blankBlocks.Count > 0)
                    {
                        foreach (KeyValuePair<ByteArrayKey, BlockPayload> blankPayload in blankBlocks)
                        {
                            hashedBlocks.Add(blankPayload);
                        }

                        blankBlocks.Clear();
                    }

                    CurrentBlockCount++;
                    ShowProgress(CurrentBlockCount, TotalBlockCount, startTime, blankPayloadPhase, Logging);
                }
            }

            Logging.Log("");
            Logging.Log($"FFU Block Count: {hashedBlocks.Count} - {hashedBlocks.Count * BlockSize / (1024 * 1024 * 1024)}GB");

            return [.. hashedBlocks];
        }
    }
}