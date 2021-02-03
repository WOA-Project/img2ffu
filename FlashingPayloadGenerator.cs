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
using System.Linq;
using System.Security.Cryptography;

namespace Img2Ffu
{
    internal class FlashingPayloadGenerator
    {
        private static void ShowProgress(Int64 CurrentProgress, Int64 TotalProgress, DateTime startTime, bool DisplayRed)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / CurrentProgress * (TotalProgress - CurrentProgress));

            Logging.Log(string.Format("{0} {1:hh\\:mm\\:ss\\.f}", GetDismLikeProgBar((Int32)(CurrentProgress * 100 / TotalProgress)), remaining, remaining.TotalHours, remaining.Minutes, remaining.Seconds, remaining.Milliseconds), returnline: false, severity: DisplayRed ? Logging.LoggingLevel.Warning : Logging.LoggingLevel.Information);
        }

        private static string GetDismLikeProgBar(Int32 perc)
        {
            Int32 eqsLength = (Int32)((Double)perc / 100 * 55);
            string bases = new string('=', eqsLength) + new string(' ', 55 - eqsLength);
            bases = bases.Insert(28, perc + "%");
            if (perc == 100)
                bases = bases.Substring(1);
            else if (perc < 10)
                bases = bases.Insert(28, " ");
            return "[" + bases + "]";
        }

        internal static FlashingPayload[] GetOptimizedPayloads(FlashPart[] flashParts, UInt32 chunkSize, UInt32 BlankSectorBufferSize)
        {
            List<FlashingPayload> flashingPayloads = new List<FlashingPayload>();

            if (flashParts == null)
                return flashingPayloads.ToArray();

            Int64 TotalProcess1 = 0;
            for (Int32 j = 0; j < flashParts.Count(); j++)
            {
                FlashPart flashPart = flashParts[j];
                TotalProcess1 += flashPart.Stream.Length / chunkSize;
            }

            Int64 CurrentProcess1 = 0;
            DateTime startTime = DateTime.Now;
            Logging.Log("Hashing resources...");

            ulong maxblank = BlankSectorBufferSize;
            bool blankphase = false;
            ulong blankcount = 0;
            List<FlashingPayload> blankbuffer = new List<FlashingPayload>();

            using (SHA256 crypto = SHA256.Create())
            {
                for (UInt32 j = 0; j < flashParts.Count(); j++)
                {
                    FlashPart flashPart = flashParts[(Int32)j];

                    flashPart.Stream.Seek(0, SeekOrigin.Begin);
                    Int64 totalChunkCount = flashPart.Stream.Length / chunkSize;

                    for (UInt32 i = 0; i < totalChunkCount; i++)
                    {
                        byte[] buffer = new byte[chunkSize];
                        Int64 position = flashPart.Stream.Position;
                        flashPart.Stream.Read(buffer, 0, (Int32)chunkSize);
                        byte[] hash = crypto.ComputeHash(buffer);

                        byte[] emptyness = new byte[] { 0xFA, 0x43, 0x23, 0x9B, 0xCE, 0xE7, 0xB9, 0x7C, 0xA6, 0x2F, 0x00, 0x7C, 0xC6, 0x84, 0x87, 0x56, 0x0A, 0x39, 0xE1, 0x9F, 0x74, 0xF3, 0xDD, 0xE7, 0x48, 0x6D, 0xB3, 0xF9, 0x8D, 0xF8, 0xE4, 0x71 };

                        if (!ByteOperations.Compare(emptyness, hash))
                        {
                            flashingPayloads.Add(new FlashingPayload(1, new byte[][] { hash }, new UInt32[] { ((UInt32)flashPart.StartLocation / chunkSize) + i }, new UInt32[] { j }, new Int64[] { position }));
                            
                            if (blankphase && blankcount < maxblank)
                            {
                                foreach (var blankpay in blankbuffer)
                                {
                                    flashingPayloads.Add(blankpay);
                                }
                            }

                            blankphase = false;
                            blankcount = 0;
                            blankbuffer.Clear();
                        }
                        else if (blankcount < maxblank)
                        {
                            blankphase = true;
                            blankcount++;
                            blankbuffer.Add(new FlashingPayload(1, new byte[][] { hash }, new UInt32[] { ((UInt32)flashPart.StartLocation / chunkSize) + i }, new UInt32[] { j }, new Int64[] { position }));
                        }
                        else if (blankcount >= maxblank && blankbuffer.Count > 0)
                        {
                            foreach (var blankpay in blankbuffer)
                            {
                                flashingPayloads.Add(blankpay);
                            }
                            blankbuffer.Clear();
                        }

                        CurrentProcess1++;
                        ShowProgress(CurrentProcess1, TotalProcess1, startTime, blankphase);
                    }
                }
            }

            return flashingPayloads.ToArray();
        }
    }
}