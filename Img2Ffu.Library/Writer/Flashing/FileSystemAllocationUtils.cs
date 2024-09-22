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
using DiscUtils;
using DiscUtils.Ntfs;

namespace Img2Ffu.Writer.Flashing
{
    internal class FileSystemAllocationUtils
    {
        public static (ulong StartOffset, ulong Length)[] GetNTFSAllocatedClustersMap(Stream fileSystemStream)
        {
            try
            {
                using NtfsFileSystem fileSystem = new(fileSystemStream);
                ulong totalClusters = (ulong)fileSystem.TotalClusters;
                ulong usedClusters = (ulong)fileSystem.UsedSpace / (ulong)fileSystem.ClusterSize;

                List<ulong> ClustersInUse = [];
                for (ulong i = 0; i < totalClusters; i++)
                {
                    if (fileSystem.IsClusterInUse((long)i))
                    {
                        ClustersInUse.Add((ulong)fileSystem.ClusterToOffset((long)i));
                    }
                }
                return GetAllocatedClustersMap(ClustersInUse, fileSystem);
            }
            catch (InvalidFileSystemException)
            {
                // Not NTFS
                throw;
            }
        }

        private static (ulong StartOffset, ulong Length)[] GetAllocatedClustersMap(List<ulong> ClustersInUse, IClusterBasedFileSystem fileSystem)
        {
            ulong sizeOfOneCluster = (ulong)fileSystem.ClusterSize;
            ClustersInUse.Sort();

            if (ClustersInUse[0] != 0)
            {
                ClustersInUse.Insert(0, ClustersInUse[0]);
            }

            List<(ulong StartOffset, ulong Length)> FileSystemParts = [];

            foreach (ulong clusterStartOffset in ClustersInUse)
            {
                if (FileSystemParts.Count == 0)
                {
                    FileSystemParts.Add((clusterStartOffset, sizeOfOneCluster));
                }
                else
                {
                    (ulong StartOffset, ulong Length) = FileSystemParts[^1];
                    if (StartOffset + Length == clusterStartOffset)
                    {
                        FileSystemParts[^1] = (StartOffset, Length + sizeOfOneCluster);
                    }
                    else
                    {
                        FileSystemParts.Add((clusterStartOffset, sizeOfOneCluster));
                    }
                }
            }

            return [.. FileSystemParts];
        }

        public static bool IsNTFS(Stream fileSystemStream)
        {
            _ = fileSystemStream.Seek(0, SeekOrigin.Begin);
            return NtfsFileSystem.Detect(fileSystemStream);
        }
    }
}
