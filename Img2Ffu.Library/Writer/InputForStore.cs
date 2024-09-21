namespace Img2Ffu.Writer
{
    public record struct InputForStore(string InputFile, string DevicePath, bool IsFixedDiskLength, uint MaximumNumberOfBlankBlocksAllowed, string[] ExcludedPartitionNames)
    {
        public static implicit operator (string InputFile, string DevicePath, bool IsFixedDiskLength, uint MaximumNumberOfBlankBlocksAllowed, string[] ExcludedPartitionNames)(InputForStore value)
        {
            return (value.InputFile, value.DevicePath, value.IsFixedDiskLength, value.MaximumNumberOfBlankBlocksAllowed, value.ExcludedPartitionNames);
        }

        public static implicit operator InputForStore((string InputFile, string DevicePath, bool IsFixedDiskLength, uint MaximumNumberOfBlankBlocksAllowed, string[] ExcludedPartitionNames) value)
        {
            return new InputForStore(value.InputFile, value.DevicePath, value.IsFixedDiskLength, value.MaximumNumberOfBlankBlocksAllowed, value.ExcludedPartitionNames);
        }
    }
}
