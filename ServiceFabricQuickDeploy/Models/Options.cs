using CommandLine;

namespace ServiceFabricQuickDeploy.Models
{
    public class Options
    {
        [Option('a', "attach", DefaultValue = true,
             HelpText = "Determines whether the Visual Studio debugger should be automatically attached after deploy")]
        public bool AttachDebugger { get; set; }

        [Option('k', "kill-processes", DefaultValue = false,
            HelpText = "Determines whether the Service Fabric processes should be forcibly killed or whether it should be reprovisioned using the Service Fabric API")]
        public bool KillProcesses { get; set; }

        [Option('p', "sf-cluster-path", DefaultValue = Constants.DefaultServiceFabricAppPath,
            HelpText = "Determines whether the Service Fabric processes should be forcibly killed or whether it should be reprovisioned using the Service Fabric API")]
        public string ServiceFabricAppPath { get; set; }
    }
}