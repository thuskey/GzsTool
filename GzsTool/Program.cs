﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GzsTool.Fpk;
using GzsTool.Gzs;
using GzsTool.PathId;

namespace GzsTool
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            ReadPs3PathIdFile("pathid_list_ps3.bin");
            Hashing.ReadDictionary("dictionary.txt");
            Hashing.ReadMd5Dictionary("fpk_dict.txt");

            if (args.Length == 1)
            {
                string path = args[0];
                if (File.Exists(path))
                {
                    if (path.EndsWith(".g0s"))
                    {
                        ReadGzsArchive(path);
                        return;
                    }
                    if (path.EndsWith(".fpk") || path.EndsWith(".fpkd"))
                    {
                        ReadFpkArchive(path);
                        return;
                    }
                }
                else if (Directory.Exists(path))
                {
                    ReadFpkArchives(path);
                    return;
                }
            }
            ShowUsageInfo();
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("GzsTool by Atvaark\n" +
                              "  A tool for unpacking g0s, fpk and fpkd files\n" +
                              "Usage:\n" +
                              "  GzsTool file_path.g0s  - Unpacks the g0s file\n" +
                              "  GzsTool file_path.fpk  - Unpacks the fpk file\n" +
                              "  GzsTool file_path.fpkd - Unpacks the fpkd file\n" +
                              "  GzsTool folder_path    - Unpacks all fpk and fpkd files in the folder\n");
        }

        private static void ReadGzsArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectory = Path.Combine(fileDirectory, fileNameWithoutExtension);


            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                GzsFile file = GzsFile.ReadGzsFile(input);
                file.ExportFiles(input, outputDirectory);
            }
        }

        private static void ReadFpkArchives(string path)
        {
            var extensions = new List<string>
            {
                ".fpk",
                ".fpkd"
            };
            var files = GetFileList(new DirectoryInfo(path), true, extensions);
            foreach (var file in files)
            {
                ReadFpkArchive(file.FullName);
            }
        }

        private static PathIdFile ReadPs3PathIdFile(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                PathIdFile pathIdFile = new PathIdFile();
                pathIdFile.Read(stream);
                return pathIdFile;
            }
        }

        private static void ReadFpkArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).Replace(".", "");
            string outputDirectory = string.Format("{0}\\{1}_{2}", fileDirectory, fileNameWithoutExtension, extension);

            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                FpkFile file = FpkFile.ReadFpkFile(input);

                foreach (var entry in file.Entries)
                {
                    string fileName = GetFpkEntryFileName(entry);
                    string outputPath = Path.Combine(outputDirectory, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                    using (FileStream output = new FileStream(outputPath, FileMode.Create))
                    {
                        output.Write(entry.Data, 0, entry.Data.Length);
                    }
                }
            }
        }

        private static string GetFpkEntryFileName(FpkEntry entry)
        {
            byte[] entryNameHash = Hashing.Md5HashText(entry.FileName.Name);
            string fileName = entryNameHash.SequenceEqual(entry.Md5Hash)
                ? entry.FileName.Name
                : Hashing.GetFileNameFromMd5Hash(entry.Md5Hash, entry.FileName.Name);
            fileName = fileName.Replace("/", "\\");
            int index = fileName.IndexOf(":", StringComparison.Ordinal);
            if (index != -1)
            {
                fileName = fileName.Substring(index + 1, fileName.Length - index - 1);
            }
            fileName = fileName.StartsWith("\\") ? fileName.Substring(1, fileName.Length - 1) : fileName;
            return fileName;
        }

        private static List<FileInfo> GetFileList(DirectoryInfo fileDirectory, bool recursively, List<string> extensions)
        {
            List<FileInfo> files = new List<FileInfo>();
            if (recursively)
            {
                foreach (var directory in fileDirectory.GetDirectories())
                {
                    files.AddRange(GetFileList(directory, recursively, extensions));
                }
            }
            files.AddRange(
                fileDirectory.GetFiles()
                    .Where(f => extensions.Contains(f.Extension, StringComparer.CurrentCultureIgnoreCase)));
            return files;
        }
    }
}