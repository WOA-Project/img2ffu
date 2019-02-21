# img2ffu - Converts raw image (img) files into full flash update (FFU) files

## Copyright

Copyright (c) 2019, Gustave Monce - gus33000.me - @gus33000

*Portions from:*
Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda

This software is released under the MIT license, for more information please see LICENSE.md

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

