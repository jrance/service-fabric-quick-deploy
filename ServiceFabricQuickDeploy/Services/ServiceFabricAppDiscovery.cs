using System;
using System.Collections.Generic;
using System.Fabric;
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
            Dictionary<string, string> serviceLookup = null;

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
                        result.AppTypeName = GetAppNameFromManifest(manifest, out serviceLookup);
                    }
                }
            }

            var fabricClient = new FabricClient();
            foreach (var project in result.ServiceFabricProjects)
            {
                project.ServiceUri = new Uri($"fabric:/{result.AppTypeName.Replace("Type", "")}/{serviceLookup[project.ServiceTypeName]}");
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

            var serviceTypesElement = doc.Root.Element(ns + "ServiceTypes");
            var serviceTypeElement = serviceTypesElement.Element(ns + "StatelessServiceType") ?? serviceTypesElement.Element(ns + "StatefulServiceType");

            result.ServiceTypeName = serviceTypeElement.Attribute("ServiceTypeName").Value;

            var programElement = codeElement.Descendants(ns + "Program").First();
            result.ProgramName = programElement.Value;
            return result;
        }

        private string GetAppNameFromManifest(FileInfo manifest, out Dictionary<string, string> serviceLookup)
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/2011/01/fabric");
            serviceLookup = new Dictionary<string, string>();
            var doc = XDocument.Load(manifest.FullName);
            if (doc == null)
                throw new InvalidOperationException($"Unable to read {manifest.FullName} file");

            foreach (var service in doc.Root.Descendants(ns + "Service"))
            {
                var name = service.Attribute("Name").Value;
                var serviceTypeName = (service.Element(ns + "StatelessService") ?? service.Element(ns + "StatefulService")).Attribute("ServiceTypeName").Value;
                serviceLookup.Add(serviceTypeName, name);
            }
            return doc.Root.Attribute("ApplicationTypeName").Value;
        }
    }
}
