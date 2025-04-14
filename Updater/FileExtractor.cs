using System.IO;
using System.IO.Compression;

namespace Singularity.Updater
{
    public static class FileExtractor
    {
        public static void ExtractZipOverwrite(string zipPath, string destinationFolder)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    string filePath = Path.Combine(destinationFolder, Path.GetFileName(entry.FullName));

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    entry.ExtractToFile(filePath);
                }
            }
        }
    }
}