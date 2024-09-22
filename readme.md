# img2ffu

Converts raw image (img) files into full flash update (FFU) files.

## Description

This tool converts a raw image dump of a Windows Phone device or any GPT image dump to a Full Flash Update (FFU) file, compatible with the v1 format and ready to be flashed on unlocked devices. FFU being built using this tool are optimized to save both space and flashing time.

During the making of the FFU image, some partitions will get ignored. Those partitions are typically not flashed during a FFU flash and should never be flashed. You should not attempt to disable this safety measure.
To be safe, it is recommended you check manually if the ignored partitions aren't present in the final FFU image, to make sure you're not going to brick your device, or cause irrecoverable damage to your device.

The tool is able to work directly on a img file, or directly on a device eMMC. Please refer to the tool help for more information.

## Flashing guide

Please always make a full device backup before attempting to flash any FFU. I recommend to use Win32DiskImager, to select one of the drive letters of your device, and back it up.

**Important**: Win32DiskImager has no concept of partitions, it will backup and write the entire eMMC. You cannot restore just one partition using this tool, and it is recommended you do not attempt to flash in mass storage mode on devices!

For factory unlocked devices, you can always flash the FFU file without any problem.
For retail devices, you must unlock your device UEFI using WPinternals.

You can then flash the FFU file you made with any tool you're familiar with.

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
