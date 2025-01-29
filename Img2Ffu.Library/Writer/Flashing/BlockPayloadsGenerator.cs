/*
 * Copyright (c) Gustave Monce and Contributors
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using Img2Ffu.Writer.Data;
using System.Collections;
using System.Security.Cryptography;

namespace Img2Ffu.Writer.Flashing
{
    internal static class BlockPayloadsGenerator
    {
        internal static List<KeyValuePair<ByteArrayKey, BlockPayload>> GetGPTPayloads(List<KeyValuePair<ByteArrayKey, BlockPayload>> blockPayloads, Stream stream, uint BlockSize, bool IsFixedDiskLength)
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
            _ = stream.Read(PrimaryGPTBuffer);

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

            // When a store has a fixed size (cannot expand or vary with end user configuration)
            // We want to write the final working GPT as a final step
            // This is useful to prevent end users from having a half written disk being written
            // and then attempted to be booted on, as it would land directly in EDL with a blank GPT
            // This behavior is default for Windows Phone and Windows Mobile and WCOS
            // On some cases, the GPT should be written not last but after all critical partitions got written.
            // For example, on eMMC layouts of WP, you generally want it written after PLAT is written to the device
            // We do not implement this just yet here.
            if (IsFixedDiskLength)
            {
                ulong endGPTChunkStartLocation = (ulong)stream.Length - BlockSize;
                byte[] SecondaryGPTBuffer = new byte[(int)BlockSize];
                _ = stream.Seek((long)endGPTChunkStartLocation, SeekOrigin.Begin);
                _ = stream.Read(SecondaryGPTBuffer);

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
            // Otherwise, directly write the GPT as second step, and last step.
            // In-between, normal data is being written, and at first, the GPT is wiped.
            // This behavior was confirmed on officially made FFUs for WCOS.
            // On some cases, the GPT should be written not last but after all critical partitions got written.
            // This is useful to prevent end users from having a half written disk being written
            // and then attempted to be booted on, as it would land directly in EDL with a blank GPT
            // For example, on eMMC layouts of WP, you generally want it written after PLAT is written to the device
            // We do not implement this just yet here.
            else
            {
                // Now add back both GPTs
                // Apparently the first one needs to be added twice?
                blockPayloadsList.Insert(1, primaryGPTKeyValuePair);
                blockPayloadsList.Add(primaryGPTKeyValuePair);

                ulong endGPTChunkStartLocation = (ulong)stream.Length - BlockSize;
                byte[] SecondaryGPTBuffer = new byte[(int)BlockSize];
                _ = stream.Seek((long)endGPTChunkStartLocation, SeekOrigin.Begin);

                try
                {
                    _ = stream.Read(SecondaryGPTBuffer);
                }
                catch (EndOfStreamException)
                {
                    // For some reason there is a bug with DiscUtils itself that can cause this exception to get thrown
                    // Simply ignore it and replace the payload with a whole lot of nothing.
                    SecondaryGPTBuffer = new byte[BlockSize];
                }
                catch
                {
                    throw;
                }


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

            return blockPayloadsList;
        }

        internal static List<KeyValuePair<ByteArrayKey, BlockPayload>> GetOptimizedPayloads(FlashPart[] flashParts, uint BlockSize, uint BlankSectorBufferSize, ILogging Logging)
        {
            List<KeyValuePair<ByteArrayKey, BlockPayload>> hashedBlocks = [];

            if (flashParts == null)
            {
                return hashedBlocks;
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

            Memory<byte> blockBuffer = new byte[BlockSize];
            Span<byte> FFUBlockPayload = blockBuffer.Span;

            foreach (FlashPart flashPart in flashParts)
            {
                _ = flashPart.Stream.Seek(0, SeekOrigin.Begin);

                ulong streamLength = (ulong)flashPart.Stream.Length;
                ulong totalBlockCount = streamLength / BlockSize;

                for (ulong blockIndex = 0; blockIndex < totalBlockCount; blockIndex++)
                {
                    ulong streamPosition = (ulong)flashPart.Stream.Position;

                    try
                    {
                        _ = flashPart.Stream.Read(FFUBlockPayload);
                    }
                    catch (EndOfStreamException)
                    {
                        // For some reason there is a bug with DiscUtils itself that can cause this exception to get thrown
                        // Simply ignore it and replace the payload with a whole lot of nothing.
                        blockBuffer = new byte[BlockSize];
                        FFUBlockPayload = blockBuffer.Span;
                    }
                    catch
                    {
                        throw;
                    }

                    byte[] FFUBlockHash = SHA256.HashData(FFUBlockPayload);

                    if (!StructuralComparisons.StructuralEqualityComparer.Equals(EMPTY_BLOCK_HASH, FFUBlockHash) ||
                        blankPayloadCount < BlankSectorBufferSize)
                    {
                        ulong FFUBlockIndex = (flashPart.StartLocation / BlockSize) + blockIndex;

                        if (FFUBlockIndex > uint.MaxValue)
                        {
                            throw new NotSupportedException("The image requires more block than the FFU format can support.");
                        }

                        KeyValuePair<ByteArrayKey, BlockPayload> blockDataKeyPair = new(
                            new ByteArrayKey(FFUBlockHash),
                            new BlockPayload(
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
                                        BlockIndex = (uint)FFUBlockIndex,
                                        DiskAccessMethod = 0
                                    }
                                    ]
                                },
                                flashPart.Stream,
                                streamPosition
                            ));

                        if (!StructuralComparisons.StructuralEqualityComparer.Equals(EMPTY_BLOCK_HASH, FFUBlockHash))
                        {
                            hashedBlocks.Add(blockDataKeyPair);

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

                            blankBlocks.Add(blockDataKeyPair);
                        }
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
                    LoggingHelpers.ShowProgress(CurrentBlockCount, TotalBlockCount, startTime, blankPayloadPhase, Logging);
                }
            }

            Logging.Log("");
            Logging.Log($"FFU Block Count: {hashedBlocks.Count} - {hashedBlocks.Count * BlockSize / (1024 * 1024 * 1024)}GB");

            return hashedBlocks;
        }
    }
}