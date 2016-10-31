using System.Collections.Generic;

namespace ServiceFabricQuickDeploy.Models
{
    public class ServiceFabricApp
    {
        public string AppTypeName { get; set; }
        public IList<ServiceFabricProject> ServiceFabricProjects { get; set; }

        public string ServiceFabricRelativeAppPath => $"{ AppTypeName}_App*";
    }
}