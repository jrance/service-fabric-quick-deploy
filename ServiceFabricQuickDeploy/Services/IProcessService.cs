using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ServiceFabricQuickDeploy.Services
{
    public interface IProcessService
    {
        ICollection<string> GetRunningProcesses(string programName);
        void KillProcesses(string programName);
    }

    public class ProcessService : IProcessService
    {
        public ICollection<string> GetRunningProcesses(string programName)
        {
            try
            {
                return Process.GetProcessesByName(programName.Replace(".exe", string.Empty))
                    .Select(p => p.MainModule.FileName)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void KillProcesses(string programName)
        {
            var processes = Process.GetProcessesByName(programName.Replace(".exe", string.Empty));
            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }
}