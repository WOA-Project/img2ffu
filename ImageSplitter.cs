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

namespace Img2Ffu
{
    internal class ImageSplitter
    {
        internal static (FlashPart[], ulong, List<GPT.Partition> partitions) GetImageSlices(Stream stream, UInt32 chunkSize)
        {
            byte[] GPTBuffer = new byte[chunkSize];
            stream.Read(GPTBuffer, 0, (Int32)chunkSize);

            UInt32 sectorsInAChunk = chunkSize / 512u;

            GPT GPT = new GPT(GPTBuffer);

            List<GPT.Partition> Partitions = GPT.Partitions;
            bool isUnlocked = GPT.GetPartition("IS_UNLOCKED") != null;
            bool isUnlockedSpecA = GPT.GetPartition("HACK") != null && GPT.GetPartition("BACKUP_BS_NV") != null;

            if (isUnlocked)
                Logging.Log("The phone is an unlocked Spec B phone, UEFI_BS_NV will be kept in the FFU image for the unlock to work");

            if (isUnlockedSpecA)
                Logging.Log("The phone is an UEFI unlocked Spec A phone, UEFI_BS_NV will be kept in the FFU image for the unlock to work");

            List<FlashPart> flashParts = new List<FlashPart>();

            Logging.Log("Partitions with a * appended are ignored partitions");
            Logging.Log("");

            bool previouswasexcluded = true;

            FlashPart currentFlashPart = null;

            ulong PlatEnd = 0;

            foreach (GPT.Partition partition in Partitions.OrderBy(x => x.FirstSector))
            {
                if (partition.Name == "PLAT")
                    PlatEnd = partition.LastSector / sectorsInAChunk;

                bool isExcluded = false;

                if (excluded.Any(x => x == partition.Name))
                {
                    isExcluded = true;
                    if (isUnlocked && partition.Name == "UEFI_BS_NV")
                        isExcluded = false;
                    if (isUnlockedSpecA && partition.Name == "UEFI_BS_NV")
                        isExcluded = false;
                }

                string outputstring = (isExcluded ? "*" : "") + partition.Name + new String(' ', 50);
                outputstring = outputstring.Insert(25, " - " + partition.FirstSector);
                outputstring = outputstring.Insert(40, " - " + partition.LastSector);
                Logging.Log(outputstring, isExcluded ? Logging.LoggingLevel.Warning : Logging.LoggingLevel.Information);

                if (isExcluded)
                {
                    previouswasexcluded = true;

                    if (currentFlashPart != null)
                    {
                        if ((currentFlashPart.StartLocation / 512) % 256 != 0)
                        {
                            Logging.Log("- The stream doesn't start on a chunk boundary, a chunk is 256 sectors", Logging.LoggingLevel.Error);
                            throw new Exception();
                        }

                        if (((currentFlashPart.Stream.Length / 512) % sectorsInAChunk) != 0)
                        {
                            Logging.Log("- The stream doesn't finish on a chunk boundary, a chunk is 256 sectors", Logging.LoggingLevel.Error);
                            throw new Exception();
                        }

                        flashParts.Add(currentFlashPart);
                        currentFlashPart = null;
                    }

                    continue;
                }

                if (previouswasexcluded)
                {
                    currentFlashPart = new FlashPart(stream, partition.FirstSector * 512);
                }

                previouswasexcluded = false;
                currentFlashPart.Stream = new PartialStream(stream, (Int64)currentFlashPart.StartLocation, (Int64)(partition.LastSector + 1) * 512);
            }

            if (!previouswasexcluded)
            {
                if (currentFlashPart != null)
                {
                    currentFlashPart.Stream = new PartialStream(stream, (Int64)currentFlashPart.StartLocation, stream.Length);

                    if ((currentFlashPart.StartLocation / 512) % 256 != 0)
                    {
                        Logging.Log("- The stream doesn't start on a chunk boundary, a chunk is 256 sectors", Logging.LoggingLevel.Error);
                        throw new Exception();
                    }

                    if (((currentFlashPart.Stream.Length / 512) % sectorsInAChunk) != 0)
                    {
                        Logging.Log("- The stream doesn't finish on a chunk boundary, a chunk is 256 sectors", Logging.LoggingLevel.Error);
                        throw new Exception();
                    }

                    flashParts.Add(currentFlashPart);
                }
            }

            Logging.Log("");
            Logging.Log("Plat end: " + PlatEnd);

            Logging.Log("");
            Logging.Log("Inserting GPT back into the FFU image");
            flashParts.Insert(0, new FlashPart(new MemoryStream(GPTBuffer), 0));

            return (flashParts.OrderBy(x => x.StartLocation).ToArray(), PlatEnd, Partitions);
        }

        private readonly static string[] excluded = new string[]
        {
            "DPP",
            "MODEM_FSG",
            "MODEM_FS1",
            "MODEM_FS2",
            "MODEM_FSC",
            "DDR",
            "SEC",
            "APDP",
            "MSADP",
            "DPO",
            "SSD",
            "DBI",
            "UEFI_BS_NV",
            "UEFI_NV",
            "UEFI_RT_NV",
            "UEFI_RT_NV_RPMB",
            "BOOTMODE",
            "LIMITS",
            "BACKUP_BS_NV",
            "BACKUP_SBL1",
            "BACKUP_SBL2",
            "BACKUP_SBL3",
            "BACKUP_PMIC",
            "BACKUP_DBI",
            "BACKUP_UEFI",
            "BACKUP_RPM",
            "BACKUP_QSEE",
            "BACKUP_QHEE",
            "BACKUP_TZ",
            "BACKUP_HYP",
            "BACKUP_WINSECAPP",
            "BACKUP_TZAPPS",
            "SVRawDump",
            "IS_UNLOCKED",
            "HACK"
        };
    }
}