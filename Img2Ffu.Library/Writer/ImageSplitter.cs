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
using static Img2Ffu.GPT;

namespace Img2Ffu.Writer
{
    internal static class ImageSplitter
    {
        private static GPT GetGPT(Stream stream, uint BlockSize, uint sectorSize, ILogging Logging)
        {
            byte[] GPTBuffer = new byte[BlockSize];
            _ = stream.Read(GPTBuffer);

            uint requiredGPTBufferSize = GetGPTSize(GPTBuffer, sectorSize);
            if (BlockSize < requiredGPTBufferSize)
            {
                string errorMessage = $"The Block size is too small to contain the GPT, the GPT is {requiredGPTBufferSize} bytes long, the Block size is {BlockSize} bytes long";
                Logging.Log(errorMessage, ILoggingLevel.Error);
                throw new Exception(errorMessage);
            }

            uint sectorsInABlock = BlockSize / sectorSize;

            GPT GPT = new(GPTBuffer, sectorSize);

            if (BlockSize > requiredGPTBufferSize && GPT.Partitions.OrderBy(x => x.FirstSector).Any(x => x.FirstSector < sectorsInABlock))
            {
                Partition conflictingPartition = GPT.Partitions.OrderBy(x => x.FirstSector).First(x => x.FirstSector < sectorsInABlock);

                string errorMessage = $"The Block size is too big to contain only the GPT, the GPT is {requiredGPTBufferSize} bytes long, the Block size is {BlockSize} bytes long. The overlapping partition is {conflictingPartition.Name} at {conflictingPartition.FirstSector * sectorSize}";
                Logging.Log(errorMessage, ILoggingLevel.Error);
                throw new Exception(errorMessage);
            }

            return GPT;
        }

        internal static (FlashPart[], List<Partition> partitions) GetImageSlices(Stream stream, uint BlockSize, string[] ExcludedPartitionNames, uint sectorSize, ILogging Logging)
        {
            GPT GPT = GetGPT(stream, BlockSize, sectorSize, Logging);
            uint sectorsInABlock = BlockSize / sectorSize;

            List<FlashPart> flashParts = FlashPartFactory.GetFlashParts(GPT, stream, BlockSize, ExcludedPartitionNames, sectorSize, Logging);

            FlashPart[] finalFlashParts = [.. flashParts];

            Logging.Log("");
            Logging.Log("Final Flash Parts");
            Logging.Log("");
            PrintFlashParts(finalFlashParts, sectorSize, BlockSize, Logging);
            Logging.Log("");

            foreach (FlashPart flashPart in finalFlashParts)
            {
                ulong totalSectors = (ulong)flashPart.Stream.Length / sectorSize;
                ulong firstSector = flashPart.StartLocation / sectorSize;
                ulong lastSector = firstSector + totalSectors - 1;

                if (firstSector % sectorsInABlock != 0)
                {
                    string errorMessage = $"- The stream doesn't start on a Block boundary (Total Sectors: {totalSectors} - First Sector: {firstSector} - Last Sector: {lastSector}) - Overflow: {firstSector % sectorsInABlock}, a Block is {sectorsInABlock} sectors";
                    Logging.Log(errorMessage, ILoggingLevel.Error);
                    throw new Exception(errorMessage);
                }

                if ((lastSector + 1) % sectorsInABlock != 0)
                {
                    string errorMessage = $"- The stream doesn't end on a Block boundary (Total Sectors: {totalSectors} - First Sector: {firstSector} - Last Sector: {lastSector}) - Overflow: {(lastSector + 1) % sectorsInABlock}, a Block is {sectorsInABlock} sectors";
                    Logging.Log(errorMessage, ILoggingLevel.Error);
                    throw new Exception(errorMessage);
                }
            }

            return (finalFlashParts, GPT.Partitions);
        }

        private static void PrintFlashParts(FlashPart[] finalFlashParts, uint sectorSize, uint BlockSize, ILogging Logging)
        {
            for (int i = 0; i < finalFlashParts.Length; i++)
            {
                FlashPart flashPart = finalFlashParts[i];
                PrintFlashPart(flashPart, sectorSize, BlockSize, $"FlashPart[{i}]", Logging);
            }
        }

        private static void PrintFlashPart(FlashPart flashPart, uint sectorSize, uint BlockSize, string name, ILogging Logging)
        {
            uint sectorsInABlock = BlockSize / sectorSize;

            ulong totalSectors = (ulong)flashPart.Stream.Length / sectorSize;
            ulong firstSector = flashPart.StartLocation / sectorSize;
            ulong lastSector = firstSector + totalSectors - 1;

            Logging.Log($"{name} - {firstSector}s - {lastSector}s - {totalSectors}s - {totalSectors / (double)sectorsInABlock}c", ILoggingLevel.Information);
        }
    }
}