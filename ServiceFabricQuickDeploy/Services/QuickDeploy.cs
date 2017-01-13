using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using ServiceFabricQuickDeploy.Logging;
using ServiceFabricQuickDeploy.Models;
using ServiceFabricQuickDeploy.ServiceManagers;

namespace ServiceFabricQuickDeploy.Services
{
    internal class QuickDeploy : IQuickDeploy
    {
        private readonly IVsEnvironment _vsEnvironment;
        private readonly IServiceManager _serviceManager;
        private readonly IProcessService _processService;
        private readonly ILogger _logger;

        internal QuickDeploy(IVsEnvironment vsEnvironment, IServiceManager serviceManager, IProcessService processService, ILogger logger)
        {
            _vsEnvironment = vsEnvironment;
            _serviceManager = serviceManager;
            _processService = processService;
            _logger = logger;
        }

        public async Task DeployAsync(ServiceFabricApp appDetails, string serviceFabricAppPath, bool attachDebugger)
        {
            var nodeDirectories = Directory.GetDirectories(serviceFabricAppPath, "_Node_*",
                SearchOption.TopDirectoryOnly);
            Parallel.ForEach(appDetails.ServiceFabricProjects, service =>
            {
                DeployServiceAsync(service, appDetails.ServiceFabricRelativeAppPath, nodeDirectories, attachDebugger).Wait();
            });
        }

        private ICollection<string> GetProgramFilesThatNeedUpdating(ServiceFabricProject service, string[] nodeDirectories,
            string serviceFabricRelativeAppPath)
        {
            var result = new List<string>();

            var newProgramFile = new FileInfo($"{service.BuildOutputPath}\\{service.ProgramName}");
            if (!newProgramFile.Exists) return null;

            foreach (var directory in nodeDirectories)
            {
                var appDirectory = Directory.GetDirectories(directory, serviceFabricRelativeAppPath,
                    SearchOption.TopDirectoryOnly);
                if (appDirectory.Length == 0) continue;

                var filePath = $"{appDirectory.Last()}\\{service.ServiceFabricRelativeServicePath}\\{service.ProgramName}";
                var existingProgramFile = new FileInfo(filePath);
                if (existingProgramFile.Exists && newProgramFile.LastWriteTimeUtc > existingProgramFile.LastWriteTimeUtc)
                {
                    result.Add(existingProgramFile.DirectoryName);
                }
            }
            return result;
        }

        private async Task DeployServiceAsync(ServiceFabricProject service, string serviceFabricRelativeAppPath, string[] nodeDirectories,
            bool attachDebugger)
        {
            ICollection<string> runningProcesses = _processService.GetRunningProcesses(service.ProgramName);
            var deploymentLocations = GetProgramFilesThatNeedUpdating(service, nodeDirectories, serviceFabricRelativeAppPath);
            if (deploymentLocations.Any())
            {
                _logger.LogInformation($"Stopping service {service.ServiceName} on local Service Fabric cluster");
                var serviceDescription = await _serviceManager.StopService(service);

                _logger.LogInformation(
                    $"Deploying build files for {service.ServiceName} to local Service Fabric cluster");

                foreach (var deploymentLocation in deploymentLocations)
                {
                    CopyFiles(service.BuildOutputPath, deploymentLocation);
                }

                _logger.LogInformation($"Starting service {service.ServiceName} on local Service Fabric cluster");
                runningProcesses = await _serviceManager.StartService(service, serviceDescription, runningProcesses.Count);
            }
            else
            {
            }

            if (attachDebugger)
            {
                await AttachToProcesses(runningProcesses);
            }
        }

        private int GetProcessInstanceCount(string processName)
        {
            var processes = Process.GetProcessesByName(processName.Replace(".exe", string.Empty));
            return processes.Length;
        }

        private async Task AttachToProcesses(ICollection<string> runningProcesses)
        {
            var sta = new StaTaskScheduler(numberOfThreads: 4);
            var taskList = new List<Task>();
            foreach(var runningProcess in runningProcesses)
            {
                taskList.Add(Task.Factory.StartNew(() =>
                    _vsEnvironment.AttachDebugger(runningProcess), CancellationToken.None, TaskCreationOptions.None, sta));
            }
            //.Select(process => Task.Factory.StartNew(() =>
            //        _vsEnvironment.AttachDebugger(processName), CancellationToken.None, TaskCreationOptions.None, sta))
            //.ToList();

            await Task.WhenAll(taskList);
        }

        private void CopyFiles(string sourcePath, string destinationPath)
        {
            foreach (var sourceFile in Directory.GetFiles(sourcePath))
            {
                var sourceFileInfo = new FileInfo(sourceFile);
                var destFileInfo = new FileInfo($"{destinationPath}\\{sourceFileInfo.Name}");

                if (!destFileInfo.Exists || sourceFileInfo.LastWriteTimeUtc > destFileInfo.LastWriteTimeUtc)
                {
                    while (true)
                    {
                        try
                        {
                            File.Copy(sourceFileInfo.FullName, destFileInfo.FullName, true);
                            break;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(200);
                        }
                    }
                }
            }
        }
    }
}