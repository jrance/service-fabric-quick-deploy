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

namespace ServiceFabricQuickDeploy.Services
{
    internal class QuickDeploy : IQuickDeploy
    {
        private readonly IVsEnvironment _vsEnvironment;
        private readonly ILogger _logger;

        internal QuickDeploy(IVsEnvironment vsEnvironment, ILogger logger)
        {
            _vsEnvironment = vsEnvironment;
            _logger = logger;
        }

        public async Task DeployAsync(ServiceFabricApp appDetails, bool attachDebugger)
        {
            var taskList = new List<Task<string>>();
            foreach (var service in appDetails.ServiceFabricProjects)
            {
                taskList.Add(Task.Run(() => DeployService(service, appDetails.ServiceFabricRelativeAppPath)));
            }

            await Task.WhenAll(taskList);

            var deployedProgramFiles = taskList.Select(t => t.Result).Where(t => t != null);

            if (attachDebugger)
            {
                await AttachToProcesses(deployedProgramFiles);
            }
        }

        private string DeployService(ServiceFabricProject service, string serviceFabricRelativeAppPath)
        {
            var nodeDirectories = Directory.GetDirectories(Constants.ServiceFabricAppPath, "_Node_*",
                SearchOption.TopDirectoryOnly);

            foreach (var directory in nodeDirectories)
            {
                var appDirectory = Directory.GetDirectories(directory, serviceFabricRelativeAppPath,
                    SearchOption.TopDirectoryOnly);
                if (appDirectory.Length == 0) continue;

                var filePath = $"{appDirectory.Last()}\\{service.ServiceFabricRelativeServicePath}\\{service.ProgramName}";
                var existingProgramFile = new FileInfo(filePath);
                if (existingProgramFile.Exists)
                {
                    var newProgramFile = new FileInfo($"{service.BuildOutputPath}\\{service.ProgramName}");
                    if (newProgramFile.Exists && newProgramFile.LastAccessTimeUtc > existingProgramFile.LastWriteTimeUtc)
                    {
                        KillProcesses(existingProgramFile.Name);

                        _logger.LogInformation($"Deploying build files for {service.ServiceName} to local Service Fabric cluster");
                        CopyFiles(newProgramFile.Directory, existingProgramFile.DirectoryName);
                    }
                    return existingProgramFile.FullName;
                }
            }
            return null;
        }

        private void KillProcesses(string processName)
        {
            var processes = Process.GetProcessesByName(processName.Replace(".exe", string.Empty));

            foreach (var process in processes)
            {
                _logger.LogInformation($"Killing process {process.MainModule.FileName}");
                process.Kill();
            }
        }

        private async Task AttachToProcesses(IEnumerable<string> processes)
        {
            var sta = new StaTaskScheduler(numberOfThreads: 4);
            var taskList = processes
                .Select(process => Task.Factory.StartNew(() =>
                        _vsEnvironment.AttachDebugger(process), CancellationToken.None, TaskCreationOptions.None, sta))
                .ToList();

            await Task.WhenAll(taskList);
        }

        private void CopyFiles(DirectoryInfo sourceDirectory, string destinationPath)
        {
            foreach (var sourceFile in sourceDirectory.GetFiles())
            {
                var destFileInfo = new FileInfo($"{destinationPath}\\{sourceFile.Name}");

                if (!destFileInfo.Exists || sourceFile.LastWriteTimeUtc > destFileInfo.LastWriteTimeUtc)
                {
                    while (true)
                    {
                        try
                        {
                            File.Copy(sourceFile.FullName, destFileInfo.FullName, true);
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(200);
                            throw;
                        }
                    }
                }
            }
        }
    }
}