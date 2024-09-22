# img2ffu

Converts raw image (img) files into full flash update (FFU) files.

## Description

This tool converts a raw image dump of a Windows Phone device or any GPT image dump to a Full Flash Update (FFU) file, compatible with the v1 format and ready to be flashed on unlocked devices. FFU being built using this tool are optimized to save both space and flashing time.

During the making of the FFU image, some partitions will get ignored. Those partitions are typically not flashed during a FFU flash and should never be flashed. You should not attempt to disable this safety measure.
To be safe, it is recommended you check manually if the ignored partitions aren't present in the final FFU image, to make sure you're not going to brick your device, or cause irrecoverable damage to your device.

The tool is able to work directly on a img file, or directly on a device eMMC. Please refer to the tool help for more information.

This tool also adds support for making v2 format FFU files, as well as parsing v1, v2 and v1 with compression ffu files.

You can see a few projects listed below making use of img2ffu library to parse FFU files:

- [ffu2vhdx](https://github.com/gus33000/ffu2vhdx)
- [MobilePackageGen](https://github.com/gus33000/MobilePackageGen)
- [UnifiedFlashingPlatform](https://github.com/WOA-Project/UnifiedFlashingPlatform)

## Flashing guide

Please always make a full device backup before attempting to flash any FFU. I recommend to use Win32DiskImager, to select one of the drive letters of your device, and back it up.

> [!IMPORTANT]
> Win32DiskImager has no concept of partitions, it will backup and write the entire eMMC. You cannot restore just one partition using this tool, and it is recommended you do not attempt to flash in mass storage mode on devices!

For factory unlocked devices, you can always flash the FFU file without any problem.
For retail devices, you must unlock your device UEFI using WPinternals.

You can then flash the FFU file you made with any tool you're familiar with.

## Samples

### Generating a FFU file for a Surface Duo (1st Gen) 128GB device containing only LUN0

```batch
img2ffu.exe ^
  --ffu-file ".\OEMEP_128GB_HalfSplit.ffu" ^
  --block-size 16384 ^
  --sector-size 4096 ^
  --plat-id "Microsoft Corporation.Surface.Surface Duo.1930" ^
  --plat-id "OEMB1.*.OEMB1 Product.*" ^
  --plat-id "OEMEP.*.OEMEP Product.*" ^
  --os-version 10.0.22621.1 ^
  --full-flash-update-version V2 ^
  --img-file ".\LUN0.vhdx" ^
  --device-path VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A) ^
  --is-fixed-disk-length false ^
  --blanksectorbuffer-size 4000 ^
  --excluded-file .\provisioning-partitions.txt
```

### Generating a FFU file for a Lumia 950 XL

```batch
img2ffu.exe ^
  --ffu-file ".\RM-1085_Flash.ffu" ^
  --block-size 131072 ^
  --sector-size 512 ^
  --plat-id "Microsoft.MSM8994.P6211" ^
  --os-version 10.0.10586.512 ^
  --full-flash-update-version V1 ^
  --img-file ".\eMMC-User.vhdx" ^
  --device-path VenHw(B615F1F5-5088-43CD-809C-A16E52487D00) ^
  --is-fixed-disk-length false ^
  --blanksectorbuffer-size 100 ^
  --excluded-file .\provisioning-partitions.txt
```

## Command Line Help

```
Img2Ffu 1.0.0+f19d28e6878697712b4a2195ab447abfda5935e8
Copyright (C) 2024 Img2Ffu

ERROR(S):
  Required option 'f, ffu-file' is missing.
  Required option 'p, plat-id' is missing.

  -f, --ffu-file                     Required. A path to the FFU file to output

  -p, --plat-id                      Required. Platform ID to use

  -a, --anti-theft-version           (Default: 1.1) Anti theft version.

  -o, --os-version                   (Default: 10.0.11111.0) Operating system version.

  -c, --block-size                   (Default: 131072) Block size to use for the FFU file

  -s, --sector-size                  (Default: 512) Sector size to use for the FFU file

  -v, --full-flash-update-version    (Default: V1) Version of the FFU file format to use, can be either V1 or V2

  -i, --img-file                     (Group: StoreInputOptions) A path to the img file to convert *OR* a PhysicalDisk
                                     path. i.e. \\.\PhysicalDrive1

  -d, --device-path                  (Group: StoreInputOptions) (Default: VenHw(B615F1F5-5088-43CD-809C-A16E52487D00))
                                     The UEFI device path to write the store onto when flashing.

  -l, --is-fixed-disk-length         (Group: StoreInputOptions) (Default: True) Specifies the disk in question is fixed
                                     and cannot have a different size on the end user target device. Means every data
                                     part of this disk is required for correct device firmware operation and must not be
                                     half flashed before a valid GPT is available on disk.

  -b, --blanksectorbuffer-size       (Group: StoreInputOptions) (Default: 100) Buffer size for the upper maximum allowed
                                     limit of blank sectors

  -e, --excluded-file                (Group: StoreInputOptions) (Default: .\provisioning-partitions.txt) A path to the
                                     file with all partitions to exclude

  --help                             Display this help screen.

  --version                          Display version information.

TIP(S):

  When specifying multiple stores, you must specify an instance of:

  img2ffu
    --img-file XXX
    --device-path XXX
    --is-fixed-disk-length XXX
    --blanksectorbuffer-size XXX
    --excluded-file XXX

  per store you want to use, in a row, and in order.

  For example, to generate a FFU file that contains two stores,
  one writing to an UFS LUN0,
  and the other to an UFS LUN1,
  the following should be specified:

  img2ffu
    --img-file D:\MyCopyOfUFSLun0.vhdx
    --device-path VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A)
    --is-fixed-disk-length false
    --blanksectorbuffer-size 100
    --excluded-file .\provisioning-partitions.txt

    --img-file D:\MyCopyOfUFSLun1.vhdx
    --device-path VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051)
    --is-fixed-disk-length true
    --blanksectorbuffer-size 100
    --excluded-file .\provisioning-partitions.txt

  In this second example, we generate a FFU file with a single store writing to the User LUN of an eMMC:

  img2ffu
    --img-file D:\MyCopyOfeMMCUser.img
    --device-path VenHw(B615F1F5-5088-43CD-809C-A16E52487D00)
    --is-fixed-disk-length true
    --blanksectorbuffer-size 100
    --excluded-file .\provisioning-partitions.txt

  A non exhaustive list of common Phone Device Paths is given below for convenience purposes.
  Please note that you can also specify other paths such as PCI paths,
  they must bind to a real path in the UEFI environment.

    VenHw(B615F1F5-5088-43CD-809C-A16E52487D00): eMMC (User)
    VenHw(12C55B20-25D3-41C9-8E06-282D94C676AD): eMMC (Boot 1)
    VenHw(6B76A6DB-0257-48A9-AA99-F6B1655F7B00): eMMC (Boot 2)
    VenHw(C49551EA-D6BC-4966-9499-871E393133CD): eMMC (RPMB)
    VenHw(B9251EA5-3462-4807-86C6-8948B1B36163): eMMC (GPP 1)
    VenHw(24F906CD-EE11-43E1-8427-DC7A36F4C059): eMMC (GPP 2)
    VenHw(5A5709A9-AC40-4F72-8862-5B0104166E76): eMMC (GPP 3)
    VenHw(A44E27C9-258E-406E-BF33-77F5F244C487): eMMC (GPP 4)
    VenHw(D1531D41-3F80-4091-8D0A-541F59236D66): SD Card (Removable)
    VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A): UFS (LUN 0)
    VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051): UFS (LUN 1)
    VenHw(EDF85868-87EC-4F77-9CDA-5F10DF2FE601): UFS (LUN 2)
    VenHw(1AE69024-8AEB-4DF8-BC98-0032DBDF5024): UFS (LUN 3)
    VenHw(D33F1985-F107-4A85-BE38-68DC7AD32CEA): UFS (LUN 4)
    VenHw(4BA1D05F-088E-483F-A97E-B19B9CCF59B0): UFS (LUN 5)
    VenHw(4ACF98F6-26FA-44D2-8132-282F2D19A4C5): UFS (LUN 6)
    VenHw(8598155F-34DE-415C-8B55-843E3322D36F): UFS (LUN 7)
    VenHw(5397474E-F75D-44B3-8E57-D9324FCF6FE1): UFS (RPMB)

EXAMPLE(S):

  img2ffu ^
    --img-file D:\MyCopyOfUFSLun0.vhdx ^
    --device-path VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A) ^
    --is-fixed-disk-length false ^
    --blanksectorbuffer-size 100 ^
    --excluded-file .\provisioning-partitions.txt ^
    --img-file D:\MyCopyOfUFSLun1.vhdx ^
    --device-path VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051) ^
    --is-fixed-disk-length true ^
    --blanksectorbuffer-size 100 ^
    --excluded-file .\provisioning-partitions.txt ^
    --full-flash-update-version V2 ^
    --ffu-file D:\FlashTestingCli.ffu ^
    --plat-id My.Super.Plat.ID ^
    --anti-theft-version 1.1 ^
    --block-size 131072 ^
    --sector-size 4096 ^
    --os-version 10.0.22621.0

  This command will create a FFU file named D:\FlashTestingCli.ffu.
  This FFU file will target a device with Platform ID My.Super.Plat.ID.
  This FFU file will contain a disk image with a sector size of 4096 bytes.
  This FFU file will claim to contain an operating system version of 10.0.22621.0.
  This FFU file will report Anti Theft Version 1.1 is supported by the contained OS
  and can be flashed on devices featuring anti theft version 1.1.
  This FFU file will use FFU version 2.

  This FFU file will contain two stores:

    One store is sourced from a local file named D:\MyCopyOfUFSLun0.vhdx
    This store is meant to be written to the UEFI Device VenHw(860845C1-BE09-4355-8BC1-30D64FF8E63A),
    UFS LUN 0 on a Qualcomm Snapdragon UEFI Platform
    This store targets a disk which can have a different total size than the input.
    This store will contain blank blocks up to 100 in a row as part of the data being written to the phone.
    This store will not include data contained within partitions named in the provisioning-partitions.txt file.

    The second store is sourced from a local file named D:\MyCopyOfUFSLun1.vhdx
    This store is meant to be written to the UEFI Device VenHw(8D90D477-39A3-4A38-AB9E-586FF69ED051),
    UFS LUN 1 on a Qualcomm Snapdragon UEFI Platform
    This store targets a disk which cannot have a different total size than the input.
    This store will contain blank blocks up to 100 in a row as part of the data being written to the phone.
    This store will not include data contained within partitions named in the provisioning-partitions.txt file.
```

## FFU File Structure

An outdated documentation detailing some of the V1 and some of the V2 FFU file format is included on page 1145 of [Windows Manufacturing Documentation, 2019](docs/Windows%20Manufacturing%20Documentation%202019.pdf). It however is incomplete and misses on both one version of the format and many details. A lot of the format was reverse engineered from existing FFU files and tooling present in the wild in this tool. Some notes of this work are attached below:

### Revisions

3 major revisions:

- V1 (Used by Windows Phone 8.X, Windows 10 Mobile, IoT Core, early Windows Holographic, Windows Image App tooling (original versions), does not support multiple stores, does not support specifying a target device path to flash onto)
- V1_COMPRESSION (Same as V1 with added compression support, mainly used by DISM /Capture-FFU)
- V2 (Support for multiple stores, specific device path targets was added in this format revision)

### Layout

- Validation Descriptor is always of size 0
- While it is possible in the struct to specify more than one Block for a BlockDataEntry it shall only be equal to 0
- The hash table contains every hash of every block in the FFU file starting from the Image Header to the end
- When using V1_COMPRESSION FFU file format, BlockDataEntry contains an extra entry of size 4 bytes
- Multiple locations for a block data entry only copies the block to multiple places

#### Legend

*: Device Targeting Information is optional

**: Only available on V1_COMPRESSION FFU file formats

***: Only available on V2 FFU file formats

#### Schema

```
+------------------------------+
|                              |
|       Security Header        |
|                              |
+------------------------------+
|                              |
|      Security Catalog        |
|                              |
+------------------------------+
|                              |
|         Hash Table           |
|                              |
+------------------------------+
|                              |
|     (Block Size) Padding     |
|                              |
+------------------------------+
|                              |
|         Image Header         |
|                              |
+------------------------------+
|              *               |
|    Image Header Extended     |
|   DeviceTargetingInfoCount   |
|                              |
+------------------------------+
|                              |
|        Image Manifest        |
|                              |
+------------------------------+
|              *               |
|  DeviceTargetInfoLengths[0]  |
|                              |
+------------------------------+
|              *               |
|  DeviceTargetInfoStrings[0]  |
|                              |
+------------------------------+
|              *               |
|            . . .             |
|                              |
+------------------------------+
|              *               |
|  DeviceTargetInfoLengths[n]  |
|                              |
+------------------------------+
|              *               |
|  DeviceTargetInfoStrings[n]  |
|                              |
+------------------------------+
|                              |
|     (Block Size) Padding     |
|                              |
+------------------------------+
|                              |
|        Store Header[0]       |
|                              |
+------------------------------+
|             * *              |
|      CompressionAlgo[0]      |
|                              |
+------------------------------+
|            * * *             |
|      Store Header Ex[0]      |
|                              |
+------------------------------+
|                              |
|   Validation Descriptor[0]   |
|                              |
+------------------------------+
|                              |
|     Write Descriptors[0]     |
|(BlockDataEntry+DiskLocations)|
+------------------------------+
|                              |
|   (Block Size) Padding[0]    |
|                              |
+------------------------------+
|            * * *             |
|            . . .             |
|                              |
+------------------------------+
|            * * *             |
|        Store Header[n]       |
|                              |
+------------------------------+
|            * * *             |
|      Store Header Ex[n]      |
|                              |
+------------------------------+
|            * * *             |
|   Validation Descriptor[n]   |
|                              |
+------------------------------+
|            * * *             |
|     Write Descriptors[n]     |
|(BlockDataEntry+DiskLocations)|
+------------------------------+
|            * * *             |
|   (Block Size) Padding[n]    |
|                              |
+------------------------------+
|                              |
|         Data Blocks          |
|                              |
+------------------------------+
```

## Copyright

Copyright (c) 2019-2024, Gustave Monce - gus33000.me - @gus33000

*Portions from:*
Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda

This software is released under the MIT license, for more information please see [LICENSE.md](./license.md)
