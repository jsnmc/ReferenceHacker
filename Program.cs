using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VSLangProj;
using EnvDTE;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using Microsoft.Build.BuildEngine;

using Mono.Options;

namespace ReferenceHacker
{
    class Program
    {
        static void Main(string[] args)
        {
            bool runMirror = false;
            bool show_help = false;           
            string existingReferenceRootPath = null;
            string targetRootPath = null;
            string globalPropertiesFile = null;
            bool verboseMode = false;

            List<string> solutionList = new List<string>();

            var p = new OptionSet() {
                { "s|source=", "a directoryy path to the folder that contains the project references",
                    (string v) => existingReferenceRootPath = v },
                { "r|dest=","a directory path in which the references will be changed to point to.",
                    (string v) => targetRootPath = v },
                { "g|globals=", "a full directory path to the global project properties file - typcically a .csproj file",
                    v => globalPropertiesFile = v },
                { "vs|vsolution=", "the full path to a Visual Studio solution to operate on ( one or more).",
                    v => solutionList.Add (v) },
                { "m|mirror", "skips this step by default, but if the option is present the root reference directory will be copied to the destination.",
                    v => runMirror = v != null },
                { "v|verbose",  "run in verbose output mode",
                    v => verboseMode = v != null },
                { "h|help",  "show this message and exit",
                    v => show_help = v != null }};

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("ReferenceHacker exception: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `ReferenceHacker --help' for more information.");
                return;
            }

            if (show_help || args.Length <4 )
            {
                ShowHelp(p);
                return;
            }

            if (!ValidateInput(existingReferenceRootPath, targetRootPath, globalPropertiesFile, solutionList))
                return;

            RunCoreLogic(existingReferenceRootPath, targetRootPath, globalPropertiesFile, solutionList, runMirror, verboseMode);

            Console.WriteLine("Run complete...");
        }

        static void RunCoreLogic(string existingReferenceRootPath, string targetRootPath, string globalPropertiesFile, List<string>solutionList, bool runMirror, bool verbose)
        {
            var existingReferencesMap = ProjectUtils.GetDllReferencePaths(existingReferenceRootPath);

            // Copy External references
            if (runMirror)
                ProjectUtils.MirrorDirectory(existingReferenceRootPath, targetRootPath, 4);

            // Register the IOleMessageFilter to handle any threading errors.
            MessageFilter.Register();

            //Process each solution passed in
            foreach (var solution in solutionList)
            {
                Console.WriteLine("Operating on VS Solution: {0}", solution);
                Microsoft.Build.Construction.SolutionFile s = Microsoft.Build.Construction.SolutionFile.Parse(solution);

                // Hack on the project files
                foreach (var proj in s.ProjectsInOrder)
                {
                    var projectFile = !Path.HasExtension(proj.AbsolutePath) ? Path.Combine(proj.AbsolutePath, proj.ProjectName) : proj.AbsolutePath;

                    if (verbose)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.WriteLine("Operating on project: {0} - Type: {1}", projectFile, proj.ProjectType);
                        Console.ResetColor();
                    }
                
                    ReferenceHackerCore.AddGlobalImport(projectFile, globalPropertiesFile);
                    ReferenceHackerCore.SetReferenceMsBuild(projectFile, existingReferencesMap, existingReferenceRootPath, "$(THIRD_PARTY_REFPATH)", verbose);
                }
            }
        }

        /// <summary>
        /// Validate command line options
        /// </summary>
        /// <param name="existingReferenceRootPath"></param>
        /// <param name="targetRootPath"></param>
        /// <param name="globalPropertiesFile"></param>
        /// <param name="solutionList"></param>
        /// <returns></returns>
        static bool ValidateInput(string existingReferenceRootPath, string targetRootPath, string globalPropertiesFile, List<string> solutionList)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            bool validated = true;
            if (string.IsNullOrEmpty(existingReferenceRootPath))
            {
                Console.WriteLine("Error - missing source direcotry argument");
                
                validated = false;
            }
            if (string.IsNullOrEmpty(targetRootPath))
            {
                Console.WriteLine("Error - missing dest direcotry argument");
                validated = false;
            }
            if (string.IsNullOrEmpty(globalPropertiesFile))
            {
                Console.WriteLine("Error - missing globals direcotry argument");
                validated = false;
            }
            if (existingReferenceRootPath == null || !Directory.Exists(existingReferenceRootPath))
            {
                Console.WriteLine("Error - {0} (source) dirctory not found", existingReferenceRootPath);
                validated = false;
            }
            if (targetRootPath == null || !Directory.Exists(targetRootPath))
            {
                Console.WriteLine("Error - {0} (dest) dirctory not found", targetRootPath);
                validated = false;
            }
            if (!File.Exists(globalPropertiesFile))
            {
                Console.WriteLine("Error - {0}  (globals) file not found", globalPropertiesFile);
                validated = false;
            }

            if (solutionList.Any())
            {
                foreach (var solution in solutionList)
                {
                    if (!File.Exists(solution))
                    {
                        Console.WriteLine("Error - {0}  (solution) file not found", solution);
                        validated = false;
                    }
                }
            }
            else
            {
                Console.WriteLine("Error - (vsolution) missing argument(s)");
                validated = false;
            }
            Console.ResetColor();
            return validated;
        }

        /// <summary>
        /// Output a friendly help message
        /// </summary>
        /// <param name="p"></param>
        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: ReferenceHacker [OPTIONS]+ message");
            Console.WriteLine("Hack on a Visual Studio solution and update external references to point to a new location - adjust project to use a global properties file.");
            Console.WriteLine("Provide the source directory that contains the current directory of external references/dlls/libs");
            Console.WriteLine("Provide the destination path in which the current external directory will be copied to");
            Console.WriteLine("Provide a path in to a global project properties file in which the externals directory will be defined and all project will reference this file.");
            Console.WriteLine("Provide a path to a Visual Studio solution to operate on");
            Console.WriteLine("Mirroring option will move the external directory to the destination path");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

    public class MessageFilter : IOleMessageFilter
    {
        //
        // Class containing the IOleMessageFilter
        // thread error-handling functions.

        // Start the filter.
        public static void Register()
        {
            IOleMessageFilter newFilter = new MessageFilter();
            IOleMessageFilter oldFilter = null;
            CoRegisterMessageFilter(newFilter, out oldFilter);
        }

        // Done with the filter, close it.
        public static void Revoke()
        {
            IOleMessageFilter oldFilter = null;
            CoRegisterMessageFilter(null, out oldFilter);
        }

        //
        // IOleMessageFilter functions.
        // Handle incoming thread requests.
        int IOleMessageFilter.HandleInComingCall(int dwCallType,
          System.IntPtr hTaskCaller, int dwTickCount, System.IntPtr
          lpInterfaceInfo)
        {
            //Return the flag SERVERCALL_ISHANDLED.
            return 0;
        }

        // Thread call was rejected, so try again.
        int IOleMessageFilter.RetryRejectedCall(System.IntPtr
          hTaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == 2)
            // flag = SERVERCALL_RETRYLATER.
            {
                // Retry the thread call immediately if return >=0 & 
                // <100.
                return 99;
            }
            // Too busy; cancel call.
            return -1;
        }

        int IOleMessageFilter.MessagePending(System.IntPtr hTaskCallee,
          int dwTickCount, int dwPendingType)
        {
            //Return the flag PENDINGMSG_WAITDEFPROCESS.
            return 2;
        }

        // Implement the IOleMessageFilter interface.
        [DllImport("Ole32.dll")]
        private static extern int
          CoRegisterMessageFilter(IOleMessageFilter newFilter, out
          IOleMessageFilter oldFilter);
    }

    [ComImport(), Guid("00000016-0000-0000-C000-000000000046"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(
            int dwCallType,
            IntPtr hTaskCaller,
            int dwTickCount,
            IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwRejectType);

        [PreserveSig]
        int MessagePending(
            IntPtr hTaskCallee,
            int dwTickCount,
            int dwPendingType);
        }
    }


}
