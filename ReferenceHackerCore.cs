using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.BuildEngine;

using VSLangProj;

namespace ReferenceHacker
{
    public class ReferenceHackerCore
    {      
        public static Project MsbuildProjEng => new Microsoft.Build.BuildEngine.Project(new Microsoft.Build.BuildEngine.Engine());

        /// <summary>
        /// Hack in the global properties file that should contain at least a refernce to THIRD_PARTY_REFPATH ( see sample file )
        /// </summary>
        /// <param name="projectFile">string - path to the .csproj project file</param>
        /// <param name="importFilePath">string - path to the global properties file</param>
        public static void AddGlobalImport( string projectFile, string importFilePath)
        {
            try
            {
                var msbuildProjEng = ReferenceHackerCore.MsbuildProjEng;
                msbuildProjEng.Load(projectFile);
                // Make sure we haven't referenced this file already
                foreach (Import import in msbuildProjEng.Imports)
                {
                    if (string.CompareOrdinal(System.IO.Path.GetFileName(import.ProjectPath), System.IO.Path.GetFileName(importFilePath)) == 0) return;
                }
                
                // Get the relative path and hack in the global file reference
                string relativeImportFilePath = ProjectUtils.MakeRelativePathFromFullPaths(projectFile, importFilePath);
                msbuildProjEng.Imports.AddNewImport(relativeImportFilePath ?? importFilePath, "true");
                msbuildProjEng.Save(projectFile);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        /// <summary>
        /// Main external reference hacking logic.  
        /// 1. Get all references in the project 
        /// 2. loop through and see if any HintPaths match up with the references found in the existingReferencesMap
        /// 3. if we have a hit, replace the path with the new environment variable path ( THIRD_PARTY_REFPATH )
        /// </summary>
        /// <param name="projectFile">string - path to .csproj to operate on</param>
        /// <param name="existingReferencesMap">list of paths to files found at the library/external path</param>
        /// <param name="existingReferenceRootPath">string - path to the library/external path</param>
        /// <param name="ReferenceVar">string - the environment varible to reference in the path</param>
        public static void SetReferenceMsBuild( string projectFile, HashSet<string> existingReferencesMap,string existingReferenceRootPath,string ReferenceVar, bool verbose)
        {
            try
            {
                var msbuildProjEng =  ReferenceHackerCore.MsbuildProjEng;
                msbuildProjEng.Load(projectFile);
                //var referencest = msbuildProjEng.GetEvaluatedItemsByName("Reference").OfType<BuildItem>();
                var directoryPath = System.IO.Path.GetDirectoryName(projectFile);

                var references =
                    msbuildProjEng.GetEvaluatedItemsByName("Reference")
                        .OfType<BuildItem>()
                        .Where(
                            item =>
                            existingReferencesMap.Contains(
                                ProjectUtils.ResolveRelativePath(
                                    directoryPath,
                                    item.GetMetadata("HintPath"))));

                var buildItems = references as IList<BuildItem> ?? references.ToList();
                
                if (buildItems.Any())
                {
                    foreach (var reference in buildItems)
                    {
                        string referenceHintPath = reference.GetMetadata("HintPath");
                        if (verbose)
                        {
                            Console.WriteLine("Found Reference: {0}", referenceHintPath);
                        }


                        var tempStr = ProjectUtils.ResolveRelativePath(directoryPath, referenceHintPath);
                        
                        var projectPropertyExpanded = tempStr.Replace(existingReferenceRootPath, ReferenceVar);
                        if (verbose)
                        {
                            Console.WriteLine("\t\tExpanded Reference: {0}", tempStr);
                            Console.WriteLine("\t\tInserted Global Property Variable: {0}", projectPropertyExpanded);
                        }

                        reference.SetMetadata("HintPath", projectPropertyExpanded);
                    }
                }
                msbuildProjEng.Save(projectFile);
            }
            catch (System.Exception ex)
            {
                if (verbose)
                {
                    Console.WriteLine("Exception: {0}", ex.Message);
                }
                else
                    System.Diagnostics.Trace.WriteLine(ex);
            }
        }

        /// <summary>
        /// Alternate implementation via EnvDTE.Project - untested
        /// </summary>
        /// <param name="project"></param>
        /// <param name="existingReferencesMap"></param>
        /// <param name="existingReferenceRootPath"></param>
        /// <param name="ReferenceVar"></param>
        public static void SetReference(EnvDTE.Project project, HashSet<string> existingReferencesMap,string existingReferenceRootPath, string ReferenceVar)
        {
            var vsproject = project.Object as VSLangProj.VSProject;
            // note: you could also try casting to VsWebSite.VSWebSite
            Dictionary<int, string> updatedRefs = new Dictionary<int, string>();
            int count = 0;
            foreach (VSLangProj.Reference reference in vsproject.References)
            {
                count++;
                Console.WriteLine(reference.Name);
                if (reference.SourceProject == null)
                {
                    if (
                        existingReferencesMap.Contains(
                            ProjectUtils.GetFullRefPathFromRelPath(existingReferenceRootPath, reference.Path)))
                    {
                        string temp = reference.Path;
                        temp = temp.Replace(existingReferenceRootPath, ReferenceVar);
                        updatedRefs.Add(count, temp);
                    }
                }
            }

            if (updatedRefs.Any())
            {

                foreach (var kvp in updatedRefs)
                {
                    Reference r = vsproject?.References.Item(kvp.Key);
                    if (r != null)
                    {
                        Console.WriteLine(r.Name);
                        try
                        {
                            r.Remove();
                            project.SaveAs(project.FileName);
                            vsproject = project.Object as VSLangProj.VSProject;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex);
                        }
                    }

                    if (vsproject != null)
                    {
                        try
                        {
                            // check if the old one was actually removed

                            // Add the new ref.
                            vsproject.References.Add(kvp.Value);
                        }
                        catch (Exception ex)
                        {
                            string message = $"Could not add Revf. \n Exception: {ex.Message}";
                            System.Diagnostics.Trace.WriteLine(message);
                        }
                    }
                }
            }
        }


        public static IEnumerable<string> CollectSettings(EnvDTE.Project project)
        {
            var vsproject = project.Object as VSLangProj.VSProject;

            foreach (VSLangProj.Reference reference in vsproject.References)
            {
                Console.WriteLine(reference.Name);
                if (reference.SourceProject == null)
                {
                    yield return reference.Path;
                }
                else
                {
                    // This is a project reference
                    Console.WriteLine(reference.Name);
                }
            }
        }
    }
}