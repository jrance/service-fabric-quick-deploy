using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using EnvDTE80;
using EnvDTE90;
using ServiceFabricQuickDeploy.Filters;
using ServiceFabricQuickDeploy.Logging;
using ServiceFabricQuickDeploy.Models;

namespace ServiceFabricQuickDeploy.Services
{
    public class VsEnvironment : IVsEnvironment, IDisposable
    {
        private readonly ILogger _logger;
        private readonly DTE2 _dte2;
        private static readonly object Lock = new object();

        public VsEnvironment(ILogger logger)
        {
            _logger = logger;
            _dte2 = GetCurrent();
            MessageFilter.Register();
        }

        public IEnumerable<VsProject> GetSolutionProjects()
        {
            var projects = new List<VsProject>();
            foreach (Project project in _dte2.Solution.Projects)
            {
                if (string.IsNullOrEmpty(project.FileName))
                {
                    projects.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    projects.Add(MapProjectToVsProject(project));
                }
            }
            return projects;
        }

        private IEnumerable<VsProject> GetSolutionFolderProjects(Project project)
        {
            var projects = new List<VsProject>();

            for (var i = 1; i <= project.ProjectItems.Count; i++)
            {
                var subProject = project.ProjectItems.Item(i).SubProject;
                if (subProject == null) continue;

                if (string.IsNullOrEmpty(subProject.FileName))
                {
                    projects.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    projects.Add(MapProjectToVsProject(subProject));
                }
            }
            return projects;
        }

        private VsProject MapProjectToVsProject(Project project)
        {
            var programName = project.Name;
            var projectPath = project.FullName.Substring(0, project.FileName.LastIndexOf('\\'));
            return new VsProject
            {
                ProjectName = programName,
                ProjectFolder = projectPath,
                BuildOutputPath = GetBuildOutputPathForProject(project, projectPath)
            };
        }

        private string GetBuildOutputPathForProject(Project project, string projectPath)
        {
            string directoryPath = null;
            var relativeOutputPath =
                project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value;
            var absoluteOutputPath = $"{projectPath}\\{relativeOutputPath}\\";

            if (!Directory.Exists(absoluteOutputPath)) return absoluteOutputPath;

            var files = Directory.GetFiles(absoluteOutputPath, "*.exe", SearchOption.AllDirectories);

            var mostRecentFileWriteData = DateTime.MinValue;
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Exists && fileInfo.LastWriteTime > mostRecentFileWriteData ||
                    (fileInfo.LastWriteTime == mostRecentFileWriteData &&
                     fileInfo.DirectoryName?.Length > directoryPath?.Length))
                {
                    directoryPath = fileInfo.DirectoryName;
                    mostRecentFileWriteData = fileInfo.LastWriteTime;
                }
            }
            return directoryPath;
        }

        private bool AttachToProcess(string processName)
        {
            MessageFilter.Register();
            var process = GetRunningProcess(processName);

            if (process != null && process.IsBeingDebugged) return true;

            if (process != null)
            {
                lock (Lock)
                {
                    if (!process.IsBeingDebugged)
                    {
                        var retryCount = 0;
                        try
                        {
                            process.Attach();
                        }
                        catch (Exception)
                        {
                            retryCount++;
                            if (retryCount > 15)
                            {
                                throw;
                            }
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void AttachDebugger(string processName)
        {
            var maxWaitTimeInSecs = 90;
            var start = DateTime.Now;

            _logger.LogInformation($"Attaching to process {processName}");
            int i = 0;
            while (true)
            {
                var success = AttachToProcess(processName);
                if (success)
                {
                    _logger.LogInformation($"Successfully attached to process {processName}");
                    return;
                }

                if (DateTime.Now.Subtract(start).Seconds > maxWaitTimeInSecs)
                {
                    throw new TimeoutException(
                        $"Failed to attach to process {processName} within {maxWaitTimeInSecs} seconds");
                }
                if (i%2 == 0)
                {
                    _logger.LogInformation($"Waiting for {processName} to start");
                }
                System.Threading.Thread.Sleep(200);
                i++;
            }
        }

        internal void DetachDebugger(string processName)
        {
            var process = GetRunningProcess(processName);

            if (process != null && process.IsBeingDebugged)
            {
                _logger.LogInformation($"Detaching debugger from process {processName}");
                process.Detach(false);
            }
        }

        private Process3 GetRunningProcess(string processName)
        {
            var maxTries = 10;
            for (int i = 0; i < maxTries; i++)
            {
                var process = _dte2.Debugger.LocalProcesses.OfType<Process3>()
                    .FirstOrDefault(p => p.Name.Equals(processName, StringComparison.OrdinalIgnoreCase));

                if(process != null) return process;

                System.Threading.Thread.Sleep(200);
            }
            return null;
        }


        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx bindCtx);

        [DllImport("ole32.dll")]
        private static extern void GetRunningObjectTable(int reserved, out IRunningObjectTable rot);

        private static DTE2 GetCurrent()
        {
            IRunningObjectTable rot;
            GetRunningObjectTable(0, out rot);
            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);

            enumMoniker.Reset();
            var fetched = IntPtr.Zero;
            var moniker = new IMoniker[1];

            while (enumMoniker.Next(1, moniker, fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);
                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);

                if (!displayName.StartsWith("!VisualStudio")) continue;

                object comObject;
                rot.GetObject(moniker[0], out comObject);
                var dte2 = (DTE2) comObject;
                Console.WriteLine(dte2.Solution.FullName);
                var solutionDir = dte2.Solution.FullName.Substring(0, dte2.Solution.FullName.LastIndexOf("\\", StringComparison.Ordinal));
                if (Environment.CurrentDirectory.TrimEnd('\\')
                        .IndexOf(solutionDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return dte2;
                }
            }
            throw new InvalidOperationException("Unable to find running Visual Studio matching solution");
        }

        public void Dispose()
        {
            MessageFilter.Revoke();
        }
    }
}