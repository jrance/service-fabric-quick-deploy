using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using ServiceFabricQuickDeploy.Models;
using ServiceFabricQuickDeploy.Services;

namespace ServiceFabricQuickDeploy.ServiceManagers
{
    public class ServiceFabricApiServiceManager : IServiceManager, IDisposable
    {
        private readonly FabricClient _fabricClient;
        private readonly IProcessService _processService;
        private static readonly object StopLock = new object();
        private static readonly object StartLock = new object();

        public ServiceFabricApiServiceManager(IProcessService processService)
        {
            _processService = processService;
            _fabricClient = new FabricClient();
        }

        public async Task<ServiceDescription> StopService(ServiceFabricProject serviceProject)
        {
            ServiceDescription serviceDescription = await
                _fabricClient.ServiceManager.GetServiceDescriptionAsync(serviceProject.ServiceUri);
            
            //_fabricClient.ApplicationManager..DeployServicePackageToNode()
            lock (StopLock)
            {
                _fabricClient.ServiceManager.DeleteServiceAsync(serviceProject.ServiceUri).Wait();
            }
            while (_processService.GetRunningProcesses(serviceProject.ProgramName).Count > 0)
            {
                Thread.Sleep(200);
            }
            return serviceDescription;
        }

        public async Task<ICollection<string>> StartService(ServiceFabricProject serviceProject, ServiceDescription serviceDescription, int instanceCount)
        {
            lock (StartLock)
            {
                _fabricClient.ServiceManager.CreateServiceAsync(serviceDescription).Wait();
            }
            ICollection<string> runningProcesses;
            while (true)
            {
                runningProcesses = _processService.GetRunningProcesses(serviceProject.ProgramName);
                if (runningProcesses.Count >= instanceCount) break;
                Thread.Sleep(200);
            }
            return runningProcesses;
        }
        

        public void Dispose()
        {
            _fabricClient?.Dispose();
        }
    }
}