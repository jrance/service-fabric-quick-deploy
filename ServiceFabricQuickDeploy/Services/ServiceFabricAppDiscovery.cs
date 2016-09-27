using EnvDTE;
using ServiceFabricQuickDeploy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ServiceFabricQuickDeploy.Services
{
    public class ServiceFabricAppDiscovery
    {
        private IVsEnvironment _vsEnvironment;

        public ServiceFabricAppDiscovery(IVsEnvironment vsEnvironment)
        {
            _vsEnvironment = vsEnvironment;
        }
        public ServiceFabricApp GetServiceFabricAppDetails()
        {
            var result = new ServiceFabricApp { ServiceFabricProjects = new List<ServiceFabricProject>() };
            
            foreach (Project project in _vsEnvironment.GetSolution().Projects)
            {
                if (string.IsNullOrEmpty(project.FullName)) continue;
                var projectName = project.Name;
                var projectPath = project.FullName.Substring(0, project.FileName.LastIndexOf('\\'));
                
                var manifest = new FileInfo(projectPath + @"\PackageRoot\ServiceManifest.xml");

                if(manifest.Exists)
                {
                    var serviceFabricProject = GetServiceFabricProjectFromManifest(manifest);
                    serviceFabricProject.BuildOutputPath = GetBuildOutputPathForProject(project, serviceFabricProject.ProgramName, projectPath);
                    result.ServiceFabricProjects.Add(serviceFabricProject);
                }
                else
                {
                    manifest = new FileInfo(projectPath + @"\ApplicationPackageRoot\ApplicationManifest.xml");
                    if(manifest.Exists)
                    {
                        result.AppTypeName = GetAppNameFromManifest(manifest);
                    }
                }
            }
            return result;
        }

        private string GetBuildOutputPathForProject(Project project, string programName, string projectPath)
        {
            string directoryPath = null;
            var relativeOutputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value;
            var files = Directory.GetFiles(projectPath + relativeOutputPath, programName, SearchOption.AllDirectories);

            var mostRecentFileWriteDate = DateTime.MinValue;
            foreach(var file in files)
            {
                var fileInfo = new FileInfo(file);
                if(fileInfo.LastWriteTime > mostRecentFileWriteDate || 
                    (fileInfo.LastWriteTime == mostRecentFileWriteDate && fileInfo.DirectoryName.Length > directoryPath.Length))
                {
                    directoryPath = fileInfo.DirectoryName;
                    mostRecentFileWriteDate = fileInfo.LastWriteTime;
                }
            }
            return directoryPath;
        }

        private ServiceFabricProject GetServiceFabricProjectFromManifest(FileInfo manifest)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/2011/01/fabric");
            var result = new ServiceFabricProject();
            var doc = XDocument.Load(manifest.FullName);
            result.ServiceName = doc.Root.Attribute("Name").Value;

            var codeElement = doc.Root.Element(ns + "CodePackage");
            result.Version = codeElement.Attribute("Version").Value;
            var programElement = codeElement.Descendants(ns + "Program").First();
            result.ProgramName = programElement.Value;
            return result;
        }

        private string GetAppNameFromManifest(FileInfo manifest)
        {
            var doc = XDocument.Load(manifest.FullName);
            return doc.Root.Attribute("ApplicationTypeName").Value;
        }
    }
}
