using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ReferenceHacker
{
    internal class ProjectUtils
    {
        public static string GetFullRefPathFromRelPath(string rootPath, string referencePath)
        {
            if (Path.IsPathRooted(referencePath)) return referencePath;

            var absolutePath = Path.Combine(rootPath, referencePath);
            return Path.GetFullPath((new Uri(absolutePath)).LocalPath);
        }

        public static string ResolveRelativePath(string referencePath, string relativePath)
        {
            Uri uri = new Uri(Path.Combine(referencePath, relativePath));
            return Path.GetFullPath(uri.AbsolutePath);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathRelativePathTo(
            [Out] StringBuilder pszPath,
            [In] string pszFrom,
            [In] FileAttributes dwAttrFrom,
            [In] string pszTo,
            [In] FileAttributes dwAttrTo);

        public static String MakeRelativePathFromFullPaths(String fromFilePath, String toFilePath)
        {
            const Int32 MAX_PATH = 260;
            StringBuilder str = new StringBuilder(MAX_PATH);
            if (string.IsNullOrEmpty(fromFilePath) || string.IsNullOrEmpty(toFilePath)) return string.Empty;

            FileInfo fi = new FileInfo(fromFilePath);
            if (fi.Directory == null) return string.Empty;

            bool b = PathRelativePathTo(str, fi.Directory.FullName, FileAttributes.Directory, toFilePath, FileAttributes.Normal);

            return str.ToString();
        }

        /// <summary>
        /// Get a list of target file types in the reference directory
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public static HashSet<string> GetDllReferencePaths(string rootPath)
        {
            HashSet<string> existingReference = new HashSet<string>();
            string[] extensions = { ".dll", ".tlb", ".olb", ".ocx", ".exe", ".manifest" };

            foreach (string file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(s => extensions.Any(ext => ext == Path.GetExtension(s))))
            {
                existingReference.Add(file);
            }

            return existingReference;
        }

        public static string NormalizePath(string path)
        {
            return
                Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();
        }

        public static void MirrorDirectory(string source, string dest, int failedRetryNumber, string logDir = null)
        {
            const string ExeName = "robocopy.exe";
            //robocopy "E:\test" \\server\public\test\ /MIR /W:20 /R:15 /LOG: \\server\public\logs 
            if (!Directory.Exists(dest))
            {
                try
                {
                    Directory.CreateDirectory(dest);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            // Check if paths end in same directory since robocopy doesn't copy the root/end of the path
            if (Path.GetFileName(NormalizePath(source)) != Path.GetFileName(NormalizePath(dest)))
            {

                try
                {
                    dest = Path.Combine(dest, Path.GetFileName(NormalizePath(source)));
                    Directory.CreateDirectory(dest);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            // just default to something reasonable
            if (failedRetryNumber <= 0) failedRetryNumber = 10;

            string commandLine = $"\"{source}\" \"{dest}\" /MIR /W:5 /R:{failedRetryNumber} /MT:4 /FFT /IPG:0";
            string logFileName;
            if (!string.IsNullOrEmpty(logDir))
            {
                string n = $"text-{DateTime.Now:yyyy-MM-dd_hh-mm-ss-tt}.log";
                logFileName = dest;
                logFileName = logFileName.Replace("\\", "_");
                logFileName = logFileName + "-" + n;
                logFileName = Path.Combine(logDir, logFileName);

                commandLine = $"{commandLine} /LOG:\"{logFileName}\" /TS /NP /TEE";
            }


            System.Diagnostics.Process process = BuildDataDriver.tools.ProcessRunner.RunProcess(ExeName, commandLine);

            if (process.HasExited)
            {


                switch (process.ExitCode)
                {
                    case 16:
                        Console.WriteLine("MirrorDirectory: Fatal Error ");
                        break;
                    case 15:
                        Console.WriteLine("MirrorDirectory: OKCOPY + FAIL + MISMATCHES + XTRA  ");
                        break;
                    case 14:
                        Console.WriteLine("MirrorDirectory: FAIL + MISMATCHES + XTRA ");
                        break;
                    case 13:
                        Console.WriteLine("MirrorDirectory: OKCOPY + FAIL + MISMATCHES ");
                        break;
                    case 12:
                        Console.WriteLine("MirrorDirectory: FAIL + MISMATCHES ");
                        break;
                    case 11:
                        Console.WriteLine("MirrorDirectory: OKCOPY + FAIL + XTRA ");
                        break;
                    case 10:
                        Console.WriteLine("MirrorDirectory: FAIL + XTRA ");
                        break;
                    case 9:
                        Console.WriteLine("MirrorDirectory: OKCOPY + FAIL ");
                        break;
                    case 8:
                        Console.WriteLine("MirrorDirectory: FAIL ");
                        break;
                    case 7:
                        Console.WriteLine("MirrorDirectory: OKCOPY + MISMATCHES + XTRA  ");
                        break;
                    case 6:
                        Console.WriteLine("MirrorDirectory: MISMATCHES + XTRA ");
                        break;
                    case 5:
                        Console.WriteLine("MirrorDirectory: OKCOPY + MISMATCHES  ");
                        break;
                    case 4:
                        Console.WriteLine("MirrorDirectory: MISMATCHES ");
                        break;
                    case 3:
                        Console.WriteLine("MirrorDirectory: OKCOPY + XTRA ");
                        break;
                    case 2:
                        Console.WriteLine("MirrorDirectory: XTRA ");
                        break;
                    case 1:
                        Console.WriteLine("MirrorDirectory: OKCOPY ");
                        break;
                    case 0:
                        Console.WriteLine("MirrorDirectory: No Change ");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Robocopy process has failed to exit,  SOURCE: {0} - Dest {1}", source, dest);
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            try
            {
                process.Close();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}