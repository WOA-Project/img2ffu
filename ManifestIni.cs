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

namespace Img2Ffu
{
    internal class ManifestIni
    {
        internal static string BuildUpManifest(FullFlash fullFlash, Store store, List<Partition> partitions)
        {
            string Manifest = "[FullFlash]\r\n";
            if (!string.IsNullOrEmpty(fullFlash.AntiTheftVersion))
                Manifest += "AntiTheftVersion  = " + fullFlash.AntiTheftVersion + "\r\n";
            if (!string.IsNullOrEmpty(fullFlash.OSVersion))
                Manifest += "OSVersion         = " + fullFlash.OSVersion + "\r\n";
            if (!string.IsNullOrEmpty(fullFlash.Description))
                Manifest += "Description       = " + fullFlash.Description + "\r\n";
            if (!string.IsNullOrEmpty(fullFlash.Version))
                Manifest += "Version           = " + fullFlash.Version + "\r\n";
            if (!string.IsNullOrEmpty(fullFlash.DevicePlatformId0))
                Manifest += "DevicePlatformId0 = " + fullFlash.DevicePlatformId0 + "\r\n";

            Manifest += "\r\n[Store]\r\n";
            Manifest += "SectorSize     = " + store.SectorSize + "\r\n";
            Manifest += "MinSectorCount = " + store.MinSectorCount + "\r\n";
            Manifest += "\r\n";

            foreach (Partition partition in partitions)
            {
                Manifest += "[Partition]\r\n";
                if (partition.RequiredToFlash.HasValue)
                    Manifest += "RequiredToFlash = " + (partition.RequiredToFlash.Value ? "True" : "False") + "\r\n";
                if (!string.IsNullOrEmpty(partition.Name))
                    Manifest += "Name            = " + partition.Name + "\r\n";
                Manifest += "UsedSectors     = " + partition.UsedSectors + "\r\n";
                if (partition.Type != null)
                    Manifest += "Type            = " + partition.Type + "\r\n";
                Manifest += "TotalSectors    = " + partition.TotalSectors + "\r\n";
                if (!string.IsNullOrEmpty(partition.Primary))
                    Manifest += "Primary         = " + partition.Primary + "\r\n";
                if (!string.IsNullOrEmpty(partition.FileSystem))
                    Manifest += "FileSystem      = " + partition.FileSystem + "\r\n";
                if (partition.ByteAlignment.HasValue)
                    Manifest += "ByteAlignment   = " + partition.ByteAlignment.Value + "\r\n";
                if (partition.ClusterSize.HasValue)
                    Manifest += "ClusterSize     = " + partition.ClusterSize.Value + "\r\n";
                if (partition.UseAllSpace.HasValue)
                    Manifest += "UseAllSpace     = " + (partition.UseAllSpace.Value ? "True" : "False") + "\r\n";
                Manifest += "\r\n";
            }

            return Manifest;
        }

        internal static List<Partition> GPTPartitionsToPartitions(List<GPT.Partition> partitions)
        {
            List<Partition> Parts = new List<Partition>();

            // TODO: implement more
            foreach (var part in partitions)
            {
                Parts.Add(new Partition() { Name = part.Name, TotalSectors = (uint)part.SizeInSectors, UsedSectors = (uint)part.SizeInSectors, RequiredToFlash = true });
            }

            return Parts;
        }

        internal static string BuildUpManifest(FullFlash fullFlash, Store store, List<GPT.Partition> partitions)
        {
            return BuildUpManifest(fullFlash, store, GPTPartitionsToPartitions(partitions));
        }
    }

    internal class FullFlash
    {
        public string AntiTheftVersion = "1.1"; // Allow flashing on all devices
        public string OSVersion;
        public string Description = "Update on: " + DateTime.Now.ToString("u") + "::\r\n";
        public string Version = "2.0";
        public string DevicePlatformId0;
    }

    internal class Store
    {
        public UInt32 SectorSize;
        public UInt32 MinSectorCount;
    }

    internal class Partition
    {
        public bool? RequiredToFlash;
        public UInt32 UsedSectors;
        public Guid Type;
        public UInt32 TotalSectors;
        public string Primary;
        public string Name;
        public string FileSystem;
        public UInt32? ByteAlignment;
        public UInt32? ClusterSize;
        public bool? UseAllSpace;
    }
}