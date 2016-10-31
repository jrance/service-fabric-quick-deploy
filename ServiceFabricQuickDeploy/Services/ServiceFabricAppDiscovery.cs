using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ServiceFabricQuickDeploy.Models;

namespace ServiceFabricQuickDeploy.Services
{
    public class ServiceFabricAppDiscovery : IServiceFabricAppDiscovery
    {
        private readonly IVsEnvironment _vsEnvironment;

        public ServiceFabricAppDiscovery(IVsEnvironment vsEnvironment)
        {
            _vsEnvironment = vsEnvironment;
        }

        public ServiceFabricApp GetServiceFabricAppDetails()
        {
            var result = new ServiceFabricApp { ServiceFabricProjects = new List<ServiceFabricProject>() };

            foreach (var project in _vsEnvironment.GetSolutionProjects())
            {
                var manifest = new FileInfo(project.ProjectFolder + @"\PackageRoot\ServiceManifest.xml");

                if (manifest.Exists)
                {
                    var serviceFabricProject = GetServiceFabricProjectFromManifest(manifest);
                    serviceFabricProject.BuildOutputPath = project.BuildOutputPath;
                    result.ServiceFabricProjects.Add(serviceFabricProject);
                }
                else
                {
                    manifest = new FileInfo(project.ProjectFolder + @"\ApplicationPackageRoot\ApplicationManifest.xml");
                    if (manifest.Exists)
                    {
                        result.AppTypeName = GetAppNameFromManifest(manifest);
                    }
                }
            }
            return result;
        }

        private ServiceFabricProject GetServiceFabricProjectFromManifest(FileSystemInfo manifest)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/2011/01/fabric");
            var result = new ServiceFabricProject();
            var doc = XDocument.Load(manifest.FullName);

            if(doc == null)
                throw new InvalidOperationException($"Unable to read {manifest.FullName} file");

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

            if (doc == null)
                throw new InvalidOperationException($"Unable to read {manifest.FullName} file");

            return doc.Root.Attribute("ApplicationTypeName").Value;
        }
    }
}
