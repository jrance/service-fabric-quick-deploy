using System;

namespace ServiceFabricQuickDeploy.Models
{
    public class ServiceFabricProject
    {
        public string ProgramName { get; set; }
        public string BuildOutputPath { get; set; }
        public string ServiceName { get; set; }
        public Uri ServiceUri { get; set; }
        public string Version { get; set; }
        public string ServiceFabricRelativeServicePath => $"{ServiceName}.Code.{Version}";
        public string ServiceTypeName { get; set; }
    }
}