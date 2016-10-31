using System.Collections.Generic;
using ServiceFabricQuickDeploy.Models;

namespace ServiceFabricQuickDeploy.Services
{
    public interface IVsEnvironment
    {
        IEnumerable<VsProject> GetSolutionProjects();
        void AttachDebugger(string process);
    }
}
