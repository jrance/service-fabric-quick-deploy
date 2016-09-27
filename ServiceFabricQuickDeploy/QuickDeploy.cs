using ServiceFabricQuickDeploy.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabricQuickDeploy
{
    public class QuickDeploy
    {
        private IVsEnvironment _vsEnvironment;

        public QuickDeploy(IVsEnvironment vsEnvironment)
        {
            _vsEnvironment = vsEnvironment;
        }

        public void Deploy(ServiceFabricApp appDetails, bool attachDebugger)
        {
            var deployedProgramFiles = new List<string>();
            foreach (var service in appDetails.ServiceFabricProjects)
            {
                var directories = Directory.GetDirectories(Constants.ServiceFabricAppPath, "_Node_*", SearchOption.TopDirectoryOnly);
                foreach(var directory in directories)
                {
                    var filePath = $"{directory}\\{appDetails.ServiceFabricRelativeAppPath}\\{service.ServiceFabricRelativeServicePath}\\{service.ProgramName}";
                    var existingProgramFile = new FileInfo(filePath);
                    if(existingProgramFile.Exists)
                    {
                        deployedProgramFiles.Add(existingProgramFile.FullName);
                        var newProgramFile = new FileInfo($"{service.BuildOutputPath}\\{service.ProgramName}");
                        if(newProgramFile.Exists && newProgramFile.LastWriteTimeUtc > existingProgramFile.LastWriteTimeUtc)
                        {
                            KillProcesses(existingProgramFile.Name);
                            CopyFiles(newProgramFile.Directory, existingProgramFile.DirectoryName);
                        }
                    }
                }
            }
            if (attachDebugger)
            {
                AttachToProcesses(deployedProgramFiles);
            }
        }

        private void KillProcesses(string processName)
        {
            var processes = Process.GetProcessesByName(processName.Replace(".exe", string.Empty));

            foreach(var process in processes)
            {
                process.Kill();
            }
        }

        private void AttachToProcesses(IEnumerable<string> processes)
        {
            foreach(var process in processes)
            {
                _vsEnvironment.AttachDebugger(process);
            }
        }

        private void CopyFiles(DirectoryInfo sourceDirectory, string destinationPath)
        {
            foreach (var sourceFile in sourceDirectory.GetFiles())
            {
                var destFileInfo = new FileInfo($"{destinationPath}\\{sourceFile.Name}");

                if (destFileInfo.Exists && sourceFile.LastWriteTimeUtc > destFileInfo.LastWriteTimeUtc)
                {
                    while (true)
                    {
                        try
                        {
                            File.Copy(sourceFile.FullName, destFileInfo.FullName, true);
                            break;
                        }
                        catch
                        {
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                }
            }
        }
    }
}
