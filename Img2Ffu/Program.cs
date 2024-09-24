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
using CommandLine;
using Img2Ffu.Writer;
using Img2Ffu.Writer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Img2Ffu
{
    internal class Program
    {
        private static string[] GetExcludedPartitionNameListForGivenTextFile(string ExcludedPartitionNamesFilePath)
        {
            if (!File.Exists(ExcludedPartitionNamesFilePath))
            {
                ExcludedPartitionNamesFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, ExcludedPartitionNamesFilePath);
            }

            if (!File.Exists(ExcludedPartitionNamesFilePath))
            {
                Logging.Log("Something happened.", LoggingLevel.Error);
                Logging.Log("We couldn't find the provisioning partition file.", LoggingLevel.Error);
                Logging.Log("Please specify one using the corresponding argument switch", LoggingLevel.Error);
                Environment.Exit(1);
                return null;
            }

            return File.ReadAllLines(ExcludedPartitionNamesFilePath);
        }

        private static void PrintTipHelp()
        {
            Console.WriteLine("  When specifying multiple stores, you must specify an instance of:");
            Console.WriteLine();
            Console.WriteLine("  img2ffu");
            Console.WriteLine("    --img-file XXX");
            Console.WriteLine("    --device-path XXX");
            Console.WriteLine("    --is-fixed-disk-length XXX");
            Console.WriteLine("    --blanksectorbuffer-size XXX");
            Console.WriteLine("    --excluded-file XXX");
            Console.WriteLine();
            Console.WriteLine("  per store you want to use, in a row, and in order.");
            Console.WriteLine();
            Console.WriteLine("  For example, to generate a FFU file that contains two stores,");
            Console.WriteLine("  one writing to an UFS LUN0,");
            Console.WriteLine("  and the other to an UFS LUN1,");
            Console.WriteLine("  the following should be specified:");
            Console.WriteLine();
            Console.WriteLine("  img2ffu");
            Console.WriteLine("    --img-file D:\\MyCopyOfUFSLun0.vhdx");
            Console.WriteLine("    --device-path VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A)");
            Console.WriteLine("    --is-fixed-disk-length false");
            Console.WriteLine("    --blanksectorbuffer-size 100");
            Console.WriteLine("    --excluded-file .\\provisioning-partitions.txt");
            Console.WriteLine();
            Console.WriteLine("    --img-file D:\\MyCopyOfUFSLun1.vhdx");
            Console.WriteLine("    --device-path VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051)");
            Console.WriteLine("    --is-fixed-disk-length true");
            Console.WriteLine("    --blanksectorbuffer-size 100");
            Console.WriteLine("    --excluded-file .\\provisioning-partitions.txt");
            Console.WriteLine();
            Console.WriteLine("  In this second example, we generate a FFU file with a single store writing to the User LUN of an eMMC:");
            Console.WriteLine();
            Console.WriteLine("  img2ffu");
            Console.WriteLine("    --img-file D:\\MyCopyOfeMMCUser.img");
            Console.WriteLine("    --device-path VenHw(B615F1F5-5088-43CD-809C-A16E52487D00)");
            Console.WriteLine("    --is-fixed-disk-length true");
            Console.WriteLine("    --blanksectorbuffer-size 100");
            Console.WriteLine("    --excluded-file .\\provisioning-partitions.txt");
            Console.WriteLine();
            Console.WriteLine("  A non exhaustive list of common Phone Device Paths is given below for convenience purposes.");
            Console.WriteLine("  Please note that you can also specify other paths such as PCI paths,");
            Console.WriteLine("  they must bind to a real path in the UEFI environment.");
            Console.WriteLine();
            Console.WriteLine("    VenHw(B615F1F5-5088-43CD-809C-A16E52487D00): eMMC (User)");
            Console.WriteLine("    VenHw(12C55B20-25D3-41C9-8E06-282D94C676AD): eMMC (Boot 1)");
            Console.WriteLine("    VenHw(6B76A6DB-0257-48A9-AA99-F6B1655F7B00): eMMC (Boot 2)");
            Console.WriteLine("    VenHw(C49551EA-D6BC-4966-9499-871E393133CD): eMMC (RPMB)");
            Console.WriteLine("    VenHw(B9251EA5-3462-4807-86C6-8948B1B36163): eMMC (GPP 1)");
            Console.WriteLine("    VenHw(24F906CD-EE11-43E1-8427-DC7A36F4C059): eMMC (GPP 2)");
            Console.WriteLine("    VenHw(5A5709A9-AC40-4F72-8862-5B0104166E76): eMMC (GPP 3)");
            Console.WriteLine("    VenHw(A44E27C9-258E-406E-BF33-77F5F244C487): eMMC (GPP 4)");
            Console.WriteLine("    VenHw(D1531D41-3F80-4091-8D0A-541F59236D66): SD Card (Removable)");
            Console.WriteLine("    VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A): UFS (LUN 0)");
            Console.WriteLine("    VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051): UFS (LUN 1)");
            Console.WriteLine("    VenHw(EDF85868-87EC-4F77-9CDA-5F10DF2FE601): UFS (LUN 2)");
            Console.WriteLine("    VenHw(1AE69024-8AEB-4DF8-BC98-0032DBDF5024): UFS (LUN 3)");
            Console.WriteLine("    VenHw(D33F1985-F107-4A85-BE38-68DC7AD32CEA): UFS (LUN 4)");
            Console.WriteLine("    VenHw(4BA1D05F-088E-483F-A97E-B19B9CCF59B0): UFS (LUN 5)");
            Console.WriteLine("    VenHw(4ACF98F6-26FA-44D2-8132-282F2D19A4C5): UFS (LUN 6)");
            Console.WriteLine("    VenHw(8598155F-34DE-415C-8B55-843E3322D36F): UFS (LUN 7)");
            Console.WriteLine("    VenHw(5397474E-F75D-44B3-8E57-D9324FCF6FE1): UFS (RPMB)");
            Console.WriteLine();
            Console.WriteLine("EXAMPLE(S):");
            Console.WriteLine();
            Console.WriteLine("  img2ffu ^");
            Console.WriteLine("    --img-file D:\\MyCopyOfUFSLun0.vhdx ^");
            Console.WriteLine("    --device-path VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A) ^");
            Console.WriteLine("    --is-fixed-disk-length false ^");
            Console.WriteLine("    --blanksectorbuffer-size 100 ^");
            Console.WriteLine("    --excluded-file .\\provisioning-partitions.txt ^");
            Console.WriteLine("    --img-file D:\\MyCopyOfUFSLun1.vhdx ^");
            Console.WriteLine("    --device-path VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051) ^");
            Console.WriteLine("    --is-fixed-disk-length true ^");
            Console.WriteLine("    --blanksectorbuffer-size 100 ^");
            Console.WriteLine("    --excluded-file .\\provisioning-partitions.txt ^");
            Console.WriteLine("    --full-flash-update-version V2 ^");
            Console.WriteLine("    --ffu-file D:\\FlashTestingCli.ffu ^");
            Console.WriteLine("    --plat-id My.Super.Plat.ID ^");
            Console.WriteLine("    --anti-theft-version 1.1 ^");
            Console.WriteLine("    --block-size 131072 ^");
            Console.WriteLine("    --sector-size 4096 ^");
            Console.WriteLine("    --os-version 10.0.22621.0");
            Console.WriteLine();
            Console.WriteLine("  This command will create a FFU file named D:\\FlashTestingCli.ffu.");
            Console.WriteLine("  This FFU file will target a device with Platform ID My.Super.Plat.ID.");
            Console.WriteLine("  This FFU file will contain a disk image with a sector size of 4096 bytes.");
            Console.WriteLine("  This FFU file will claim to contain an operating system version of 10.0.22621.0.");
            Console.WriteLine("  This FFU file will report Anti Theft Version 1.1 is supported by the contained OS");
            Console.WriteLine("  and can be flashed on devices featuring anti theft version 1.1.");
            Console.WriteLine("  This FFU file will use FFU version 2.");
            Console.WriteLine();
            Console.WriteLine("  This FFU file will contain two stores:");
            Console.WriteLine();
            Console.WriteLine("    One store is sourced from a local file named D:\\MyCopyOfUFSLun0.vhdx");
            Console.WriteLine("    This store is meant to be written to the UEFI Device VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A),");
            Console.WriteLine("    UFS LUN 0 on a Qualcomm Snapdragon UEFI Platform");
            Console.WriteLine("    This store targets a disk which can have a different total size than the input.");
            Console.WriteLine("    This store will contain blank blocks up to 100 in a row as part of the data being written to the phone.");
            Console.WriteLine("    This store will not include data contained within partitions named in the provisioning-partitions.txt file.");
            Console.WriteLine();
            Console.WriteLine("    The second store is sourced from a local file named D:\\MyCopyOfUFSLun1.vhdx");
            Console.WriteLine("    This store is meant to be written to the UEFI Device VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051),");
            Console.WriteLine("    UFS LUN 1 on a Qualcomm Snapdragon UEFI Platform");
            Console.WriteLine("    This store targets a disk which cannot have a different total size than the input.");
            Console.WriteLine("    This store will contain blank blocks up to 100 in a row as part of the data being written to the phone.");
            Console.WriteLine("    This store will not include data contained within partitions named in the provisioning-partitions.txt file.");
        }

        private static void Main(string[] args)
        {
            _ = new Parser(settings =>
            {
                settings.AllowMultiInstance = true;
                settings.EnableDashDash = true;
                settings.HelpWriter = Console.Error;
            }).ParseArguments<Options>(args).WithNotParsed(a =>
            {
                Console.WriteLine("TIP(S):");
                Console.WriteLine();
                PrintTipHelp();
            }).WithParsed(o =>
            {
                Logging.Log("img2ffu - Converts raw image (img) files into full flash update (FFU) files");
                Logging.Log("Copyright (c) 2019-2024, Gustave Monce - gus33000.me - @gus33000");
                Logging.Log("Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda");
                Logging.Log("Released under the MIT license at github.com/WOA-Project/img2ffu");
                Logging.Log("");

                try
                {
                    IEnumerable<string> inputFiles = o.InputFile;
                    IEnumerable<string> devicePaths = o.DevicePath;
                    IEnumerable<bool> isFixedDiskLengths = o.IsFixedDiskLength;
                    IEnumerable<uint> maximumNumberOfBlankBlocksAllowedValues = o.MaximumNumberOfBlankBlocksAllowed;
                    IEnumerable<string> excludedPartitionNamesFilePaths = o.ExcludedPartitionNamesFilePath;

                    if (inputFiles.Count() != devicePaths.Count() ||
                        inputFiles.Count() != isFixedDiskLengths.Count() ||
                        inputFiles.Count() != maximumNumberOfBlankBlocksAllowedValues.Count() ||
                        inputFiles.Count() != excludedPartitionNamesFilePaths.Count())
                    {
                        Console.WriteLine("  Unfortunately, we detected an inconsistency in the number of parameters");
                        Console.WriteLine("  you specified for each of these grouped 5 input options, and we cannot continue any further.");
                        Console.WriteLine("  Recheck your command line arguments or the help and try again after rectifying your options.");
                        Environment.Exit(1);
                        return;
                    }

                    Logging.Log("Full Flash Update Version Specified: " + o.FlashUpdateVersion);
                    Logging.Log("Number of Stores Specified: " + inputFiles.Count());
                    Logging.Log("");

                    for (int StoreIndex = 0; StoreIndex < inputFiles.Count(); StoreIndex++)
                    {
                        Logging.Log($"[Store #{StoreIndex + 1}] Input image: {inputFiles.ElementAt(StoreIndex)}");
                        Logging.Log($"[Store #{StoreIndex + 1}] Device Path: {devicePaths.ElementAt(StoreIndex)}");
                        Logging.Log($"[Store #{StoreIndex + 1}] Is Fixed Disk Length: {isFixedDiskLengths.ElementAt(StoreIndex)}");
                        Logging.Log($"[Store #{StoreIndex + 1}] Maximum number of blank blocks allowed: {maximumNumberOfBlankBlocksAllowedValues.ElementAt(StoreIndex)}");
                        Logging.Log($"[Store #{StoreIndex + 1}] Excluded Partition file: {excludedPartitionNamesFilePaths.ElementAt(StoreIndex)}");
                        Logging.Log("");
                    }

                    InputForStore[] InputsForStores = inputFiles.Select(
                        (string InputFile, int index) => 
                            new InputForStore(
                                InputFile, 
                                devicePaths.ElementAt(index),
                                isFixedDiskLengths.ElementAt(index),
                                maximumNumberOfBlankBlocksAllowedValues.ElementAt(index),
                                GetExcludedPartitionNameListForGivenTextFile(excludedPartitionNamesFilePaths.ElementAt(index))
                            )
                    ).ToArray();

                    LoggingImplementation logging = new();

                    List<DeviceTargetInfo> deviceTargetingInformations = [];

                    FFUFactory.GenerateFFU(
                        InputsForStores,
                        o.OutputFFUFile,
                        o.PlatformIDs,
                        o.SectorSize,
                        o.BlockSize,
                        o.AntiTheftVersion,
                        o.OperatingSystemVersion,
                        o.FlashUpdateVersion,
                        deviceTargetingInformations,
                        logging);
                }
                catch (Exception ex)
                {
                    Logging.Log("Something happened.", LoggingLevel.Error);
                    Logging.Log(ex.ToString(), LoggingLevel.Error);
                    Environment.Exit(1);
                }
            });
        }
    }
}