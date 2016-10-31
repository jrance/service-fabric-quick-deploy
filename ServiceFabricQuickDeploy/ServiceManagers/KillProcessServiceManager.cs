using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using ServiceFabricQuickDeploy.Models;
using ServiceFabricQuickDeploy.Services;

namespace ServiceFabricQuickDeploy.ServiceManagers
{
    public class KillProcessServiceManager : IServiceManager
    {
        private readonly IProcessService _processService;

        public KillProcessServiceManager(IProcessService processService)
        {
            _processService = processService;
        }
        public Task<ServiceDescription> StopService(ServiceFabricProject serviceProject)
        {
            _processService.KillProcesses(serviceProject.ProgramName);
            return Task.FromResult<ServiceDescription>(null);
        }
        
        public async Task<ICollection<string>> StartService(ServiceFabricProject serviceProject, ServiceDescription serviceDescription, int instanceCount)
        {
            var fabricClient = new FabricClient();
            await fabricClient.ClusterManager.RecoverPartitionsAsync();

            ICollection<string> runningProcesses;
            while (true)
            {
                runningProcesses = _processService.GetRunningProcesses(serviceProject.ProgramName);
                if (runningProcesses.Count >= instanceCount) break;
                Thread.Sleep(200);
            }
            return runningProcesses;
        }
    }
}