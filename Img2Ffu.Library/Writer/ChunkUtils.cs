namespace Img2Ffu.Writer
{
    internal static class ChunkUtils
    {
        internal static void RoundUpToChunks(FileStream stream, uint chunkSize)
        {
            long Size = stream.Length;
            if ((Size % chunkSize) > 0)
            {
                long padding = (uint)(((Size / chunkSize) + 1) * chunkSize) - Size;
                stream.Write(new byte[padding], 0, (int)padding);
            }
        }

        internal static void RoundUpToChunks(MemoryStream stream, uint chunkSize)
        {
            long Size = stream.Length;
            if ((Size % chunkSize) > 0)
            {
                long padding = (uint)(((Size / chunkSize) + 1) * chunkSize) - Size;
                stream.Write(new byte[padding], 0, (int)padding);
            }
        }
    }
}
