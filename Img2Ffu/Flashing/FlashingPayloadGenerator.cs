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
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Img2Ffu.Data;
using Img2Ffu.Helpers;

namespace Img2Ffu.Flashing
{
    internal class FlashingPayloadGenerator
    {
        private static void ShowProgress(long CurrentProgress, long TotalProgress, DateTime startTime, bool DisplayRed)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / CurrentProgress * (TotalProgress - CurrentProgress));

            Logging.Log(string.Format($"{GetDismLikeProgBar(int.Parse((CurrentProgress * 100 / TotalProgress).ToString()))} {remaining.TotalHours}:{remaining.Minutes}:{remaining.Seconds}.{remaining.Milliseconds}"), returnline: false, severity: DisplayRed ? Logging.LoggingLevel.Warning : Logging.LoggingLevel.Information);
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

        internal static FlashingPayload[] GetOptimizedPayloads(FlashPart[] flashParts, uint BlockSize, uint MaximumNumberOfBlankBlocksAllowed)
        {
            List<FlashingPayload> flashingPayload = [];

            if (flashParts == null)
            {
                return [.. flashingPayload];
            }

            long TotalBlockCount = 0;
            foreach (FlashPart flashPart in flashParts)
            {
                TotalBlockCount += flashPart.Stream.Length / BlockSize;
            }

            long CurrentBlockCount = 0;
            DateTime startTime = DateTime.Now;
            Logging.Log("Hashing resources...");

            bool blankBlockPhase = false;
            ulong blankBlockCount = 0;
            List<FlashingPayload> blankBlocks = [];

            for (uint flashPartIndex = 0; flashPartIndex < flashParts.Length; flashPartIndex++)
            {
                FlashPart flashPart = flashParts[(int)flashPartIndex];

                flashPart.Stream.Seek(0, SeekOrigin.Begin);
                long blockCount = flashPart.Stream.Length / BlockSize;

                for (uint blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    byte[] blockBuffer = new byte[BlockSize];
                    long streamLocation = flashPart.Stream.Position;
                    flashPart.Stream.Read(blockBuffer, 0, (int)BlockSize);
                    byte[] blockHash = SHA256.HashData(blockBuffer);

                    WriteDescriptor writeDescriptor = new()
                    {
                        BlockDataEntry = new BlockDataEntry
                        {
                            BlockCount = 1,
                            LocationCount = 1
                        },

                        DiskLocations = [new DiskLocation()
                        {
                            BlockIndex = (uint)flashPart.StartLocation / BlockSize + blockIndex,
                            DiskAccessMethod = 0
                        }]
                    };

                    if (!ByteOperations.Compare(EMPTY_BLOCK_HASH, blockHash))
                    {
                        flashingPayload.Add(new FlashingPayload([blockHash], [writeDescriptor], [flashPartIndex], [streamLocation]));

                        if (blankBlockPhase && blankBlockCount < MaximumNumberOfBlankBlocksAllowed)
                        {
                            // Add the last recorded blank blocks
                            foreach (FlashingPayload blankBlock in blankBlocks)
                            {
                                flashingPayload.Add(blankBlock);
                            }
                        }

                        blankBlocks.Clear();

                        blankBlockPhase = false;
                        blankBlockCount = 0;
                    }
                    else if (blankBlockCount < MaximumNumberOfBlankBlocksAllowed)
                    {
                        blankBlocks.Add(new FlashingPayload([blockHash], [writeDescriptor], [flashPartIndex], [streamLocation]));

                        blankBlockPhase = true;
                        blankBlockCount++;
                    }
                    else if (blankBlockCount >= MaximumNumberOfBlankBlocksAllowed && blankBlocks.Count > 0)
                    {
                        // Add the last recorded blank blocks and clear the list
                        foreach (FlashingPayload blankBlock in blankBlocks)
                        {
                            flashingPayload.Add(blankBlock);
                        }
                        blankBlocks.Clear();
                    }

                    CurrentBlockCount++;
                    ShowProgress(CurrentBlockCount, TotalBlockCount, startTime, blankBlockPhase);
                }
            }

            return [.. flashingPayload];
        }
    }
}