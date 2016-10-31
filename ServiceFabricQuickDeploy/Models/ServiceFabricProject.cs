namespace ServiceFabricQuickDeploy.Models
{
    public class ServiceFabricProject
    {
        public string ServiceFabricRelativeServicePath { get; set; }
        public string ProgramName { get; set; }
        public string BuildOutputPath { get; set; }
        public string ServiceName { get; set; }
        public string Version { get; set; }
    }
}