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
