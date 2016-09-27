using System.Collections.Generic;

namespace ServiceFabricQuickDeploy.Models
{
    public class ServiceFabricApp
    {
        public string AppTypeName { get; set; }
        public ICollection<ServiceFabricProject> ServiceFabricProjects{ get; set; }
        public string ServiceFabricRelativeAppPath
        {
            get
            {
                return $"{AppTypeName}_App1/";
            }
        }
    }
    public class ServiceFabricProject
    {
        public string ServiceName { get; set; }
        public string ProgramName { get; set; }
        public string Version { get; set; }
        public string BuildOutputPath { get; set; }
        public string ServiceFabricRelativeServicePath
        {
            get
            {
                return $"{ServiceName}.Code.{Version}/";
            }
        }
    }

    public class Constants
    {
        internal const string ServiceFabricAppPath = @"C:\SfDevCluster\Data\_App\";
    }
}
