using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using ReferenceHacker;

namespace ReferenceHackerTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ProjectUtilsGetFullRefPathFromRelPath()
        {
            string rootPath = @"C:\Mydocs\";
            string refPath = rootPath + "MyTest.dll";
            var ret = ProjectUtils.GetFullRefPathFromRelPath(rootPath, refPath);
            Assert.AreEqual(refPath, ret);

            //ret = ProjectUtils.GetFullRefPathFromRelPath(@"D:\TFS_Data\Ringtail", @"..\..\..\..\Ringtail.ProcessFramework.Predict.IcuTokenizer.dll");

        }

        [TestMethod]
        public void ProjectUtilsNormalizePath()
        {
            string rootPath = @"C:\Mydocs\blah\";
            var ret = ProjectUtils.NormalizePath(rootPath);
            Assert.AreEqual(@"C:\MYDOCS\BLAH", ret);
        }


        /// <summary>
        /// Run the logic to insert the global props reference and validate that it is inserted
        /// in the test project.
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"2013", @"2013")]
        public void GlobalProps()
        {
            // SETUP
            var propsFilePath = @"..\..\GLOBAL_TEST.csproj";
            var project = Path.GetFullPath(@"2013\ClassLibrary1\ClassLibrary1.csproj");
            Assert.IsTrue(File.Exists(project));

            // ACT
            ReferenceHackerCore.AddGlobalImport(project, propsFilePath);
            
            // EXPECT
            //<Import Condition="true" Project="..\..\GLOBAL_TEST.csproj" />
            XElement root = XElement.Load(project);
            Assert.IsNotNull(root);

            var importNode = from el in root.Descendants()
                            where
                                el.HasAttributes 
                                && (el.Attribute("Project") != null)
                                && (!string.IsNullOrEmpty(el.Attribute("Project").Value )
                                && (string.CompareOrdinal(el.Attribute("Project").Value, propsFilePath) == 0))
                            select el;

            Assert.IsNotNull(root);
            // No duplicates expected - logic in method shouldn't insert if it already has reference
            Assert.AreEqual(1, importNode.Count());
        }
    }
}
