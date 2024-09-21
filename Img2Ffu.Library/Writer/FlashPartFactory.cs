using static Img2Ffu.GPT;

namespace Img2Ffu.Writer
{
    internal static class FlashPartFactory
    {
        private const string ESP_FILE_PATH = @"C:\a\adaptationkits\CDG\Output\BS_EFIESP.img";
        private const string WIN_FILE_PATH = @"C:\a\adaptationkits\CDG\Output\OSPool.img";

        internal static List<FlashPart> GetFlashParts(GPT GPT, uint sectorSize)
        {
            List<Partition> Partitions = GPT.Partitions;

            List<FlashPart> flashParts = [];

            FileStream espStream = File.OpenRead(ESP_FILE_PATH);
            FileStream winStream = File.OpenRead(WIN_FILE_PATH);

            Partition esp = Partitions.First(x => x.Name.Equals("esp"));
            Partition win = Partitions.First(x => x.Name.Equals("win"));

            flashParts.Add(new FlashPart(espStream, esp.FirstSector * sectorSize));
            flashParts.Add(new FlashPart(winStream, win.FirstSector * sectorSize));

            return flashParts;
        }
    }
}
