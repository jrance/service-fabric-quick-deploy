using System.Collections.Generic;

namespace ServiceFabricQuickDeploy.Models
{
    public class ServiceFabricApp
    {
        public IList<ServiceFabricProject> ServiceFabricProjects { get; set; }
        public string ServiceFabricRelativeAppPath { get; set; }
        public string AppTypeName { get; set; }
    }
}