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
using Img2Ffu.Data;
using Img2Ffu.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Img2Ffu.Flashing
{
    internal class BlockPayloadsGenerator
    {
        private static void ShowProgress(ulong CurrentProgress, ulong TotalProgress, DateTime startTime)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            double milliseconds = timeSoFar.TotalMilliseconds / CurrentProgress * (TotalProgress - CurrentProgress);
            double ticks = milliseconds * TimeSpan.TicksPerMillisecond;
            if ((ticks > long.MaxValue) || (ticks < long.MinValue) || double.IsNaN(ticks))
            {
                milliseconds = 0;
            }
            TimeSpan remaining = TimeSpan.FromMilliseconds(milliseconds);

            Logging.Log(string.Format($"{GetDismLikeProgBar(int.Parse((CurrentProgress * 100 / TotalProgress).ToString()))} {Math.Truncate(remaining.TotalHours):00}:{remaining.Minutes:00}:{remaining.Seconds:00}.{remaining.Milliseconds:000}"), returnline: false, severity: Logging.LoggingLevel.Information);
        }

        private static string GetDismLikeProgBar(int perc)
        {
            int eqsLength = (int)((double)perc / 100 * 55);
            string bases = new string('=', eqsLength) + new string(' ', 55 - eqsLength);
            bases = bases.Insert(28, perc + "%");
            if (perc == 100)
            {
                bases = bases[1..];
            }
            else if (perc < 10)
            {
                bases = bases.Insert(28, " ");
            }

            return $"[{bases}]";
        }

        private static readonly byte[] EMPTY_BLOCK_HASH = [0xFA, 0x43, 0x23, 0x9B, 0xCE, 0xE7, 0xB9, 0x7C, 0xA6, 0x2F, 0x00, 0x7C, 0xC6, 0x84, 0x87, 0x56, 0x0A, 0x39, 0xE1, 0x9F, 0x74, 0xF3, 0xDD, 0xE7, 0x48, 0x6D, 0xB3, 0xF9, 0x8D, 0xF8, 0xE4, 0x71];

        internal static KeyValuePair<ByteArrayKey, List<ulong>>[] GetHashedBlocks(FlashPart[] flashParts, uint BlockSize)
        {
            ulong CurrentBlockCount = 0;
            ulong TotalBlockCount = 0;
            foreach (FlashPart flashPart in flashParts)
            {
                TotalBlockCount += (ulong)flashPart.Stream.Length / BlockSize;
            }
            Dictionary<ByteArrayKey, List<ulong>> hashedBlocks = [];
            DateTime startTime = DateTime.Now;

            Logging.Log($"Total Block Count: {TotalBlockCount} - {TotalBlockCount * BlockSize / (1024 * 1024 * 1024)}GB");
            Logging.Log("Hashing resources...");

            using (SHA256CryptoServiceProvider sha256CryptoServiceProvider = new())
            {
                foreach (FlashPart flashPart in flashParts)
                {
                    _ = flashPart.Stream.Seek(0, SeekOrigin.Begin);

                    ulong FlashPartStartBlockIndex = flashPart.StartLocation / BlockSize;
                    ulong FlashPartBlockCount = (ulong)flashPart.Stream.Length / BlockSize;

                    Console.WriteLine("");
                    Console.WriteLine("============================");
                    Console.WriteLine(FlashPartStartBlockIndex);
                    Console.WriteLine(FlashPartBlockCount);
                    Console.WriteLine("============================");

                    for (uint FlashPartBlockIndex = 0; FlashPartBlockIndex < FlashPartBlockCount; FlashPartBlockIndex++)
                    {
                        byte[] BlockBuffer = new byte[BlockSize];
                        flashPart.Stream.ReadExactly(BlockBuffer, 0, (int)BlockSize);
                        byte[] BlockHash = sha256CryptoServiceProvider.ComputeHash(BlockBuffer);

                        ulong DiskBlockIndex = FlashPartStartBlockIndex + FlashPartBlockIndex;

                        ByteArrayKey byteArrayKey = new(BlockHash);
                        if (hashedBlocks.TryGetValue(byteArrayKey, out List<ulong>? value))
                        {
                            value.Add(DiskBlockIndex);
                        }
                        else
                        {
                            hashedBlocks.Add(byteArrayKey, [DiskBlockIndex]);
                        }

                        CurrentBlockCount++;
                        ShowProgress(CurrentBlockCount, TotalBlockCount, startTime);
                    }
                }
            }

            Logging.Log("");
            Logging.Log($"FFU Block Count: {hashedBlocks.Count} - {hashedBlocks.Count * BlockSize / (1024 * 1024 * 1024)}GB");

            return [.. hashedBlocks];
        }

        internal static KeyValuePair<ByteArrayKey, BlockPayload>[] GetOptimizedPayloads(FlashPart[] flashParts, uint BlockSize, ulong MaximumNumberOfBlankBlocksAllowed)
        {
            List<KeyValuePair<ByteArrayKey, BlockPayload>> blockPayloads = [];

            if (flashParts == null)
            {
                return [.. blockPayloads];
            }

            KeyValuePair<ByteArrayKey, List<ulong>>[] hashedBlocks = GetHashedBlocks(flashParts, BlockSize);

            Logging.Log("Building resource payloads...");

            foreach (KeyValuePair<ByteArrayKey, List<ulong>> hashedBlock in hashedBlocks)
            {
                if (hashedBlock.Value.Count > 0)
                {
                    DiskLocation[] diskLocations = hashedBlock.Value.Select(DiskBlockIndex => new DiskLocation() { BlockIndex = (uint)DiskBlockIndex, DiskAccessMethod = 0 }).ToArray();

                    WriteDescriptor writeDescriptor = new()
                    {
                        BlockDataEntry = new BlockDataEntry
                        {
                            BlockCount = 1,
                            LocationCount = (uint)hashedBlock.Value.Count
                        },
                        DiskLocations = diskLocations
                    };

                    bool found = false;
                    for (ulong flashPartIndex = 0; flashPartIndex < (ulong)flashParts.LongLength; flashPartIndex++)
                    {
                        FlashPart flashPart = flashParts[flashPartIndex];
                        ulong DiskBlockIndex = diskLocations[0].BlockIndex;

                        ulong FlashPartStartBlockIndex = flashPart.StartLocation / BlockSize;
                        ulong FlashPartBlockCount = (ulong)flashPart.Stream.Length / BlockSize;
                        ulong FlashPartEndBlockIndex = FlashPartStartBlockIndex + FlashPartBlockCount - 1;

                        if (FlashPartStartBlockIndex <= DiskBlockIndex && DiskBlockIndex <= FlashPartEndBlockIndex)
                        {
                            ulong streamLocation = (DiskBlockIndex - FlashPartStartBlockIndex) * BlockSize;

                            blockPayloads.Add(new(hashedBlock.Key, new BlockPayload(writeDescriptor, flashPartIndex, streamLocation)));
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new Exception("Could not find flash part for block");
                    }
                }
            }

            return [.. blockPayloads];
        }
    }
}