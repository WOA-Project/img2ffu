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
using Img2Ffu.Writer.Streams;
using static Img2Ffu.GPT;

namespace Img2Ffu.Writer
{
    internal static class FlashPartFactory
    {
        private static readonly string IS_UNLOCKED_PARTITION_NAME = "IS_UNLOCKED";
        private static readonly string HACK_PARTITION_NAME = "HACK";
        private static readonly string BACKUP_BS_NV_PARTITION_NAME = "BACKUP_BS_NV";
        private static readonly string UEFI_BS_NV_PARTITION_NAME = "UEFI_BS_NV";

        private static (ulong StartOffset, ulong Length)[] GetBlockAlignedAllocationMap((ulong StartOffset, ulong Length)[] AllocationMap, ulong TotalSizeInBytes, ulong BlockSize)
        {
            List<(ulong StartOffset, ulong Length)> AllocatedBlocks = [];

            (ulong MaxStartOffset, ulong MaxLength) = AllocationMap.MaxBy(x => x.StartOffset + x.Length);
            ulong MaxEndOffset = MaxStartOffset + MaxLength;

            ulong MaxNumberOfBlocks = (MaxEndOffset / BlockSize) + 1;

            (ulong MinStartOffset, ulong MinLength) = AllocationMap.MinBy(x => x.StartOffset);
            ulong MinNumberOfBlocks = MinStartOffset / BlockSize;

            for (ulong CurrentBlockIndex = MinNumberOfBlocks; CurrentBlockIndex < MaxNumberOfBlocks; CurrentBlockIndex++)
            {
                ulong CurrentBlockStartOffset = CurrentBlockIndex * BlockSize;
                ulong CurrentBlockEndOffset = CurrentBlockStartOffset + BlockSize;

                if (AllocationMap.Any(Allocation => CurrentBlockEndOffset > Allocation.StartOffset && Allocation.StartOffset + Allocation.Length > CurrentBlockStartOffset))
                {
                    if (AllocatedBlocks.Count == 0)
                    {
                        AllocatedBlocks.Add((CurrentBlockStartOffset, BlockSize));
                    }
                    else
                    {
                        (ulong LastStartOffset, ulong LastLength) = AllocatedBlocks[^1];
                        if (LastStartOffset + LastLength == CurrentBlockStartOffset)
                        {
                            AllocatedBlocks[^1] = (LastStartOffset, LastLength + BlockSize);
                        }
                        else
                        {
                            AllocatedBlocks.Add((CurrentBlockStartOffset, BlockSize));
                        }
                    }
                }
            }

            ulong NewAllocationTotalSizeInBytes = AllocatedBlocks[^1].StartOffset + AllocatedBlocks[^1].Length;

            if (NewAllocationTotalSizeInBytes > TotalSizeInBytes)
            {
                AllocatedBlocks[^1] = (AllocatedBlocks[^1].StartOffset, AllocatedBlocks[^1].Length - (NewAllocationTotalSizeInBytes - TotalSizeInBytes));
            }

            return [.. AllocatedBlocks];
        }

        internal static List<FlashPart> GetFlashParts(GPT GPT, Stream stream, uint BlockSize, string[] ExcludedPartitionNames, uint sectorSize, ILogging Logging)
        {
            uint sectorsInABlock = BlockSize / sectorSize;

            Logging.Log($"Sector Size: {sectorSize}");
            Logging.Log($"Block Size: {BlockSize}");
            Logging.Log($"Sectors in a Block: {sectorsInABlock}");

            List<Partition> Partitions = GPT.Partitions;

            bool isUnlocked = GPT.GetPartition(IS_UNLOCKED_PARTITION_NAME) != null;
            bool isUnlockedSpecA = GPT.GetPartition(HACK_PARTITION_NAME) != null && GPT.GetPartition(BACKUP_BS_NV_PARTITION_NAME) != null;

            if (isUnlocked)
            {
                Logging.Log($"The phone is an unlocked Spec B phone, {UEFI_BS_NV_PARTITION_NAME} will be kept in the FFU image for the unlock to work");
            }

            if (isUnlockedSpecA)
            {
                Logging.Log($"The phone is an UEFI unlocked Spec A phone, {UEFI_BS_NV_PARTITION_NAME} will be kept in the FFU image for the unlock to work");
            }

            List<FlashPart> flashParts = [];

            Logging.Log("Partitions with a * appended are ignored partitions");
            Logging.Log("");

            int maxPartitionNameSize = Partitions.Select(x => x.Name.Length).Max() + 1;
            int maxPartitionLastSector = Partitions.Select(x => x.LastSector.ToString().Length).Max() + 1;

            Logging.Log($"{"Name".PadRight(maxPartitionNameSize)} - " +
                $"{"First".PadRight(maxPartitionLastSector)} - " +
                $"{"Last".PadRight(maxPartitionLastSector)} - " +
                $"{"Sectors".PadRight(maxPartitionLastSector)} - " +
                $"{"Blocks".PadRight(maxPartitionLastSector)}",
                ILoggingLevel.Information);
            Logging.Log("");

            ulong CurrentStartingOffset = 0;
            ulong CurrentEndingOffset = 0;
            List<(ulong StartOffset, ulong Length)> AllocatedPartitionsMap = [];

            foreach (Partition Partition in Partitions.OrderBy(x => x.FirstSector))
            {
                bool IsPartitionExcluded = false;

                if (ExcludedPartitionNames.Any(x => x == Partition.Name))
                {
                    IsPartitionExcluded = true;
                    if (isUnlocked && Partition.Name == UEFI_BS_NV_PARTITION_NAME)
                    {
                        IsPartitionExcluded = false;
                    }

                    if (isUnlockedSpecA && Partition.Name == UEFI_BS_NV_PARTITION_NAME)
                    {
                        IsPartitionExcluded = false;
                    }
                }

                string name = $"{(IsPartitionExcluded ? "*" : "")}{Partition.Name}";

                Logging.Log($"{name.PadRight(maxPartitionNameSize)} - " +
                    $"{(Partition.FirstSector + "s").PadRight(maxPartitionLastSector)} - " +
                    $"{(Partition.LastSector + "s").PadRight(maxPartitionLastSector)} - " +
                    $"{(Partition.SizeInSectors + "s").PadRight(maxPartitionLastSector)} - " +
                    $"{((Partition.SizeInSectors / (double)sectorsInABlock) + "c").PadRight(maxPartitionLastSector)}",
                    IsPartitionExcluded ? ILoggingLevel.Warning : ILoggingLevel.Information);

                ulong CurrentPartitionStartingOffset = Partition.FirstSector * sectorSize;
                ulong CurrentPartitionEndingOffset = (Partition.LastSector + 1) * sectorSize;

                if (IsPartitionExcluded)
                {
                    if (AllocatedPartitionsMap.Count != 0)
                    {
                        ulong AllocationTotalSizeInBytes = CurrentEndingOffset - CurrentStartingOffset;
                        (ulong StartOffset, ulong Length)[] AllocatedBlocks = GetBlockAlignedAllocationMap([.. AllocatedPartitionsMap], AllocationTotalSizeInBytes, BlockSize);

                        foreach ((ulong StartOffset, ulong Length) in AllocatedBlocks)
                        {
                            ulong blockStartOffset = CurrentStartingOffset + StartOffset;
                            PartialStream blockStream = new(stream, (long)blockStartOffset, (long)(blockStartOffset + Length));
                            FlashPart flashPart = new(blockStream, blockStartOffset);
                            flashParts.Add(flashPart);
                        }

                        AllocatedPartitionsMap.Clear();
                        CurrentStartingOffset = 0;
                        CurrentEndingOffset = 0;
                    }
                }
                else
                {
                    // 0 would be the GPT, we can't land in this case here because we deal with partitions
                    if (CurrentStartingOffset == 0)
                    {
                        CurrentStartingOffset = CurrentPartitionStartingOffset;
                        CurrentEndingOffset = CurrentPartitionEndingOffset;
                    }
                    else
                    {
                        CurrentEndingOffset = CurrentPartitionEndingOffset;
                    }

                    ulong allocationOffset = CurrentPartitionStartingOffset - CurrentStartingOffset;

                    PartialStream partialStream = new(stream, (long)CurrentPartitionStartingOffset, (long)CurrentPartitionEndingOffset);

                    /*if (FileSystemAllocationUtils.IsNTFS(partialStream))
                    {
                        (ulong StartOffset, ulong Length)[] AllocatedClusterMap = FileSystemAllocationUtils.GetNTFSAllocatedClustersMap(partialStream);
                        //AllocatedPartitionsMap.AddRange(AllocatedClusterMap.Select(x => (allocationOffset + x.StartOffset, x.Length)));

                        (ulong StartOffset, ulong Length) lastElement = AllocatedClusterMap.MaxBy(x => x.StartOffset);
                        AllocatedPartitionsMap.Add((allocationOffset, lastElement.StartOffset + lastElement.Length));
                    }
                    else*/
                    {
                        AllocatedPartitionsMap.Add((allocationOffset, Partition.SizeInSectors * sectorSize));
                    }
                }
            }

            if (AllocatedPartitionsMap.Count != 0)
            {
                (ulong StartOffset, ulong Length)[] AllocatedBlocks = GetBlockAlignedAllocationMap([.. AllocatedPartitionsMap], CurrentStartingOffset - CurrentEndingOffset, BlockSize);

                foreach ((ulong StartOffset, ulong Length) in AllocatedBlocks)
                {
                    ulong blockStartOffset = CurrentStartingOffset + StartOffset;
                    PartialStream blockStream = new(stream, (long)blockStartOffset, (long)(blockStartOffset + Length));
                    FlashPart flashPart = new(blockStream, blockStartOffset);
                    flashParts.Add(flashPart);
                }

                AllocatedPartitionsMap.Clear();
                CurrentStartingOffset = 0;
                CurrentEndingOffset = 0;
            }

            return flashParts;
        }
    }
}
