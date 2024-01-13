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
using Img2Ffu.Helpers;
using Img2Ffu.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Img2Ffu
{
    internal class ImageSplitter
    {
        internal static (FlashPart[], ulong, List<GPT.Partition> partitions) GetImageSlices(Stream stream, UInt32 chunkSize, string[] excluded, UInt32 sectorSize)
        {
            byte[] GPTBuffer = new byte[chunkSize];
            stream.Read(GPTBuffer, 0, (Int32)chunkSize);

            UInt32 sectorsInAChunk = chunkSize / sectorSize;

            GPT GPT = new(GPTBuffer, sectorSize);

            List<GPT.Partition> Partitions = GPT.Partitions;
            bool isUnlocked = GPT.GetPartition("IS_UNLOCKED") != null;
            bool isUnlockedSpecA = GPT.GetPartition("HACK") != null && GPT.GetPartition("BACKUP_BS_NV") != null;

            if (isUnlocked)
            {
                Logging.Log("The phone is an unlocked Spec B phone, UEFI_BS_NV will be kept in the FFU image for the unlock to work");
            }

            if (isUnlockedSpecA)
            {
                Logging.Log("The phone is an UEFI unlocked Spec A phone, UEFI_BS_NV will be kept in the FFU image for the unlock to work");
            }

            List<FlashPart> flashParts = [];

            Logging.Log("Partitions with a * appended are ignored partitions");
            Logging.Log("");

            bool previousWasExcluded = true;

            FlashPart currentFlashPart = null;

            ulong EndOfPLATPartition = 0;

            foreach (GPT.Partition partition in Partitions.OrderBy(x => x.FirstSector))
            {
                if (partition.Name == "PLAT")
                {
                    EndOfPLATPartition = partition.LastSector / sectorsInAChunk;
                }

                bool isExcluded = false;

                if (excluded.Any(x => x == partition.Name))
                {
                    isExcluded = true;
                    if (isUnlocked && partition.Name == "UEFI_BS_NV")
                    {
                        isExcluded = false;
                    }

                    if (isUnlockedSpecA && partition.Name == "UEFI_BS_NV")
                    {
                        isExcluded = false;
                    }
                }

                string outputString = (isExcluded ? "*" : "") + partition.Name + new String(' ', 50);
                outputString = outputString.Insert(25, " - " + partition.FirstSector);
                outputString = outputString.Insert(40, " - " + partition.LastSector);
                Logging.Log(outputString, isExcluded ? Logging.LoggingLevel.Warning : Logging.LoggingLevel.Information);

                if (isExcluded)
                {
                    previousWasExcluded = true;

                    if (currentFlashPart != null)
                    {
                        if ((currentFlashPart.StartLocation / sectorSize) % sectorsInAChunk != 0)
                        {
                            Logging.Log($"- The stream doesn't start on a chunk boundary, a chunk is {sectorsInAChunk} sectors", Logging.LoggingLevel.Error);
                            throw new Exception();
                        }

                        if (((currentFlashPart.Stream.Length / sectorSize) % sectorsInAChunk) != 0)
                        {
                            Logging.Log($"- The stream doesn't finish on a chunk boundary, a chunk is {sectorsInAChunk} sectors", Logging.LoggingLevel.Error);
                            throw new Exception();
                        }

                        flashParts.Add(currentFlashPart);
                        currentFlashPart = null;
                    }

                    continue;
                }

                if (previousWasExcluded)
                {
                    currentFlashPart = new FlashPart(stream, partition.FirstSector * sectorSize);
                }

                previousWasExcluded = false;
                currentFlashPart.Stream = new PartialStream(stream, (Int64)currentFlashPart.StartLocation, (Int64)(partition.LastSector + 1) * sectorSize);
            }

            if (!previousWasExcluded)
            {
                if (currentFlashPart != null)
                {
                    currentFlashPart.Stream = new PartialStream(stream, (Int64)currentFlashPart.StartLocation, stream.Length);

                    if ((currentFlashPart.StartLocation / sectorSize) % sectorsInAChunk != 0)
                    {
                        Logging.Log($"- The stream doesn't start on a chunk boundary, a chunk is {sectorsInAChunk} sectors", Logging.LoggingLevel.Error);
                        throw new Exception();
                    }

                    if (((currentFlashPart.Stream.Length / sectorSize) % sectorsInAChunk) != 0)
                    {
                        Logging.Log($"- The stream doesn't finish on a chunk boundary, a chunk is {sectorsInAChunk} sectors", Logging.LoggingLevel.Error);
                        throw new Exception();
                    }

                    flashParts.Add(currentFlashPart);
                }
            }

            Logging.Log("");
            Logging.Log("Plat end: " + EndOfPLATPartition);

            Logging.Log("");
            Logging.Log("Inserting GPT back into the FFU image");
            flashParts.Insert(0, new FlashPart(new MemoryStream(GPTBuffer), 0));

            return (flashParts.OrderBy(x => x.StartLocation).ToArray(), EndOfPLATPartition, Partitions);
        }
    }
}