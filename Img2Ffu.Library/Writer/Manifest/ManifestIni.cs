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
namespace Img2Ffu.Writer.Manifest
{
    internal class ManifestIni
    {
        internal static string BuildUpFullFlashSection(FullFlashManifest fullFlash)
        {
            string FullFlashSection = "[FullFlash]\r\n";

            if (!string.IsNullOrEmpty(fullFlash.OSVersion))
            {
                FullFlashSection += $"OSVersion         = {fullFlash.OSVersion}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.AntiTheftVersion))
            {
                FullFlashSection += $"AntiTheftVersion  = {fullFlash.AntiTheftVersion}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.Description))
            {
                FullFlashSection += $"Description       = {fullFlash.Description}\r\n";
            }

            FullFlashSection += "\r\n";

            if (!string.IsNullOrEmpty(fullFlash.StateSeparationLevel))
            {
                FullFlashSection += $"StateSeparationLevel = {fullFlash.StateSeparationLevel}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.UEFI))
            {
                FullFlashSection += $"UEFI                 = {fullFlash.UEFI}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.Version))
            {
                FullFlashSection += $"Version           = {fullFlash.Version}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.DevicePlatformId3))
            {
                FullFlashSection += $"DevicePlatformId3 = {fullFlash.DevicePlatformId3}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.DevicePlatformId2))
            {
                FullFlashSection += $"DevicePlatformId2 = {fullFlash.DevicePlatformId2}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.DevicePlatformId1))
            {
                FullFlashSection += $"DevicePlatformId1 = {fullFlash.DevicePlatformId1}\r\n";
            }

            if (!string.IsNullOrEmpty(fullFlash.DevicePlatformId0))
            {
                FullFlashSection += $"DevicePlatformId0 = {fullFlash.DevicePlatformId0}\r\n";
            }

            FullFlashSection += "\r\n";

            return FullFlashSection;
        }

        internal static string BuildUpStoreSection(StoreManifest store)
        {
            string StoreSection = "[Store]\r\n";
            StoreSection += $"OnlyAllocateDefinedGptEntries = False\r\n";
            StoreSection += $"StoreId                       = {{5a585bae-900b-41b5-b736-a4cecffc34b4}}\r\n";
            StoreSection += $"IsMainOSStore                 = False\r\n";
            StoreSection += $"DevicePath                    = VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A)\r\n";
            StoreSection += $"StoreType                     = Default\r\n";
            StoreSection += $"MinSectorCount                = {store.MinSectorCount}\r\n";
            StoreSection += $"SectorSize                    = {store.SectorSize}\r\n";
            StoreSection += "\r\n";
            return StoreSection;
        }

        internal static string BuildUpPartitionSection(PartitionManifest partition)
        {
            string PartitionSection = "[Partition]\r\n";

            if (partition.ByteAlignment.HasValue)
            {
                PartitionSection += $"ByteAlignment   = {partition.ByteAlignment.Value}\r\n";
            }

            if (!string.IsNullOrEmpty(partition.FileSystem))
            {
                PartitionSection += $"FileSystem      = {partition.FileSystem}\r\n";
            }

            PartitionSection += $"UsedSectors     = {partition.UsedSectors}\r\n";

            if (partition.UseAllSpace.HasValue)
            {
                PartitionSection += $"UseAllSpace     = {(partition.UseAllSpace.Value ? "True" : "False")}\r\n";
            }

            if (partition.Type != null)
            {
                PartitionSection += $"Type            = {{{partition.Type.Value.ToString().ToUpper()}}}\r\n";
            }

            PartitionSection += $"TotalSectors    = {partition.TotalSectors}\r\n";

            if (partition.RequiredToFlash.HasValue)
            {
                PartitionSection += $"RequiredToFlash = {(partition.RequiredToFlash.Value ? "True" : "False")}\r\n";
            }

            if (!string.IsNullOrEmpty(partition.Primary))
            {
                PartitionSection += $"Primary         = {partition.Primary}\r\n";
            }

            if (!string.IsNullOrEmpty(partition.Name))
            {
                PartitionSection += $"Name            = {partition.Name}\r\n";
            }

            if (partition.ClusterSize.HasValue)
            {
                PartitionSection += $"ClusterSize     = {partition.ClusterSize.Value}\r\n";
            }

            PartitionSection += "\r\n";

            return PartitionSection;
        }

        internal static string BuildUpStoragePoolSection()
        {
            string StoragePoolSection = "[StoragePool]\r\n";
            StoragePoolSection += "Name = OSPool\r\n";
            StoragePoolSection += "\r\n";
            return StoragePoolSection;
        }

        internal static string BuildUpManifest(FullFlashManifest fullFlash, StoreManifest store, List<PartitionManifest> partitions)
        {
            string Manifest = "";

            Manifest += BuildUpFullFlashSection(fullFlash);
            Manifest += BuildUpStoragePoolSection();
            Manifest += BuildUpStoreSection(store);

            foreach (PartitionManifest partition in partitions)
            {
                Manifest += BuildUpPartitionSection(partition);
            }

            return Manifest;
        }

        internal static List<PartitionManifest> GPTPartitionsToPartitions(List<GPT.Partition> partitions)
        {
            List<PartitionManifest> Parts = [];

            // TODO: implement more
            foreach (GPT.Partition part in partitions)
            {
                Parts.Add(new PartitionManifest() { Name = part.Name, TotalSectors = (uint)part.SizeInSectors, UsedSectors = (uint)part.SizeInSectors, RequiredToFlash = true, Type = part.PartitionTypeGuid });
            }

            return Parts;
        }

        internal static string BuildUpManifest(FullFlashManifest fullFlash, StoreManifest store, List<GPT.Partition> partitions)
        {
            return BuildUpManifest(fullFlash, store, GPTPartitionsToPartitions(partitions));
        }
    }
}