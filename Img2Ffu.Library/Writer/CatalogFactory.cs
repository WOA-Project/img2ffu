﻿/*
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
using System.Diagnostics;
using System.Security.Cryptography;

namespace Img2Ffu.Writer
{
    internal static class CatalogFactory
    {
        internal static byte[] GenerateCatalogFile(Span<byte> hashData)
        {
            byte[] catalog_first_part = [0x30, 0x82, 0x01, 0x44, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02, 0xA0, 0x82, 0x01, 0x35, 0x30, 0x82, 0x01, 0x31, 0x02, 0x01, 0x01, 0x31, 0x00, 0x30, 0x82, 0x01, 0x26, 0x06, 0x09, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0A, 0x01, 0xA0, 0x82, 0x01, 0x17, 0x30, 0x82, 0x01, 0x13, 0x30, 0x0C, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x01, 0x01, 0x04, 0x10, 0xA8, 0xCA, 0xD9, 0x7D, 0xBF, 0x6D, 0x67, 0x4D, 0xB1, 0x4D, 0x62, 0xFB, 0xE6, 0x26, 0x22, 0xD4, 0x17, 0x0D, 0x32, 0x30, 0x30, 0x31, 0x31, 0x30, 0x31, 0x32, 0x31, 0x32, 0x32, 0x37, 0x5A, 0x30, 0x0E, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x01, 0x02, 0x05, 0x00, 0x30, 0x81, 0xD1, 0x30, 0x81, 0xCE, 0x04, 0x1E, 0x48, 0x00, 0x61, 0x00, 0x73, 0x00, 0x68, 0x00, 0x54, 0x00, 0x61, 0x00, 0x62, 0x00, 0x6C, 0x00, 0x65, 0x00, 0x2E, 0x00, 0x62, 0x00, 0x6C, 0x00, 0x6F, 0x00, 0x62, 0x00, 0x00, 0x00, 0x31, 0x81, 0xAB, 0x30, 0x45, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x01, 0x04, 0x31, 0x37, 0x30, 0x35, 0x30, 0x10, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x01, 0x19, 0xA2, 0x02, 0x80, 0x00, 0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00, 0x04, 0x14];
            byte[] catalog_second_part = [0x30, 0x62, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x0C, 0x02, 0x02, 0x31, 0x54, 0x30, 0x52, 0x1E, 0x4C, 0x00, 0x7B, 0x00, 0x44, 0x00, 0x45, 0x00, 0x33, 0x00, 0x35, 0x00, 0x31, 0x00, 0x41, 0x00, 0x34, 0x00, 0x32, 0x00, 0x2D, 0x00, 0x38, 0x00, 0x45, 0x00, 0x35, 0x00, 0x39, 0x00, 0x2D, 0x00, 0x31, 0x00, 0x31, 0x00, 0x44, 0x00, 0x30, 0x00, 0x2D, 0x00, 0x38, 0x00, 0x43, 0x00, 0x34, 0x00, 0x37, 0x00, 0x2D, 0x00, 0x30, 0x00, 0x30, 0x00, 0x43, 0x00, 0x30, 0x00, 0x34, 0x00, 0x46, 0x00, 0x43, 0x00, 0x32, 0x00, 0x39, 0x00, 0x35, 0x00, 0x45, 0x00, 0x45, 0x00, 0x7D, 0x02, 0x02, 0x02, 0x00, 0x31, 0x00];

            byte[] hash = SHA1.HashData(hashData);

            byte[] catalog = new byte[catalog_first_part.Length + hash.Length + catalog_second_part.Length];
            Buffer.BlockCopy(catalog_first_part, 0, catalog, 0, catalog_first_part.Length);
            Buffer.BlockCopy(hash, 0, catalog, catalog_first_part.Length, hash.Length);
            Buffer.BlockCopy(catalog_second_part, 0, catalog, catalog_first_part.Length + hash.Length, catalog_second_part.Length);

            return catalog;
        }

        internal static Span<byte> GenerateCatalogFile2(byte[] hashData)
        {
            string catalog = Path.GetTempFileName();
            string cdf = Path.GetTempFileName();
            string hashTableBlob = Path.GetTempFileName();

            File.WriteAllBytes(hashTableBlob, hashData);

            using (StreamWriter streamWriter = new(cdf))
            {
                streamWriter.WriteLine("[CatalogHeader]");
                streamWriter.WriteLine("Name={0}", catalog);
                streamWriter.WriteLine("[CatalogFiles]");
                streamWriter.WriteLine("{0}={1}", "HashTable.blob", hashTableBlob);
            }

            using (Process process = new())
            {
                process.StartInfo.FileName = "MakeCat.exe";
                process.StartInfo.Arguments = string.Format("\"{0}\"", cdf);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;

                _ = process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception();
                }
            }

            Span<byte> catalogBuffer = File.ReadAllBytes(catalog);

            File.Delete(catalog);
            File.Delete(hashTableBlob);
            File.Delete(cdf);

            return catalogBuffer;
        }

        internal static byte[] SignCatalogFile(byte[] catalogData, string secureBootSigningCommand)
        {
            string catalog = Path.GetTempFileName();
            File.WriteAllBytes(catalog, catalogData);

            using (Process process = new())
            {
                string programFileName = secureBootSigningCommand.Split(' ')[0];
                string programArguments = $"{secureBootSigningCommand.AsSpan(programFileName.Length)} \"{catalog}\"".Trim();

                process.StartInfo.FileName = programFileName;
                process.StartInfo.Arguments = programArguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;

                _ = process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception();
                }
            }

            byte[] catalogBuffer = File.ReadAllBytes(catalog);

            File.Delete(catalog);

            return catalogBuffer;
        }
    }
}
