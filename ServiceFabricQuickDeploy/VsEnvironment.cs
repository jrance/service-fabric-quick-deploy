using EnvDTE;
using EnvDTE80;
using EnvDTE90;
using Newtonsoft.Json;
using ServiceFabricQuickDeploy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace ServiceFabricQuickDeploy
{
    public class VsEnvironment : IVsEnvironment, IDisposable
    {
        private DTE2 _dte2;

        public VsEnvironment()
        {
            _dte2 = GetCurrent();
            //_dte2 = (DTE2)Marshal.GetActiveObject("VisualStudio.DTE.14.0");

            MessageFilter.Register();
        }

        public Solution GetSolution()
        {
            return _dte2.Solution;
        }
        
        public bool AttachToProcess(string processName)
        {
            MessageFilter.Register();
            var process = GetRunningProcess(processName);

            if (process != null && process.IsBeingDebugged) return true;

            if (process != null)
            {
                process.Attach2("Managed");
                //Console.WriteLine("Attached to " + process.Name);
                return true;
            }
            return false;
        }

        private Process3 GetRunningProcess(string processName)
        {
            var processes = System.Diagnostics.Process.GetProcesses();
            System.Diagnostics.Process process = null;
            foreach(var p in processes)
            {
                if(processName.EndsWith(p.ProcessName + ".exe") && p.MainModule.FileName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    process = p;
                    break;
                }
            }
            if (process != null)
            {
                var maxTries = 10;
                for (int i = 0; i < maxTries; i++)
                {
                    MessageFilter.Register();
                    var proc3 = _dte2.Debugger.LocalProcesses.OfType<Process3>().FirstOrDefault(p => p.ProcessID == process.Id);
                    if (proc3 != null)
                    {
                        return proc3;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
            return null;
        }

        public void DetachDebugger(Process3 process)
        {
            if (process != null && process.IsBeingDebugged)
            {
                Console.WriteLine(process.IsBeingDebugged);
                process.Detach();
            }
        }
        public void DetachDebugger(string processName)
        {
            DetachDebugger(GetRunningProcess(processName));
        }

        public void AttachDebugger(string processName)
        {
            Console.WriteLine("Attaching to process {0}", processName);
            var i = 0;
            while (true)
            {
                var success = AttachToProcess(processName);
                if (success) return;

                if (i % 10 == 0)
                {
                    var output = $"Waiting for {processName} to start";
                    Console.WriteLine(output, processName);
                }
                System.Threading.Thread.Sleep(200);
                i++;
            }
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);
        [DllImport("ole32.dll")]
        private static extern void GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        /// <summary>
        /// Gets the current visual studio's solution DTE2
        /// </summary>
        private static DTE2 GetCurrent()
        {
            List<DTE2> dte2s = new List<DTE2>();

            IRunningObjectTable rot;
            GetRunningObjectTable(0, out rot);
            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            enumMoniker.Reset();
            IntPtr fetched = IntPtr.Zero;
            IMoniker[] moniker = new IMoniker[1];
            while (enumMoniker.Next(1, moniker, fetched) == 0)
            {
                IBindCtx bindCtx;
                CreateBindCtx(0, out bindCtx);
                string displayName;
                moniker[0].GetDisplayName(bindCtx, null, out displayName);
                // add all VisualStudio ROT entries to list
                if (displayName.StartsWith("!VisualStudio"))
                {
                    object comObject;
                    rot.GetObject(moniker[0], out comObject);
                    dte2s.Add((DTE2)comObject);
                }
            }

            // get path of the executing assembly (assembly that holds this code) - you may need to adapt that to your setup
            string thisPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // compare dte solution paths to find best match
            KeyValuePair<DTE2, int> maxMatch = new KeyValuePair<DTE2, int>(null, 0);
            foreach (DTE2 dte2 in dte2s)
            {
                int matching = GetMatchingCharsFromStart(thisPath, dte2.Solution.FullName);
                if (matching > maxMatch.Value)
                    maxMatch = new KeyValuePair<DTE2, int>(dte2, matching);
            }

            return maxMatch.Key;
        }

        /// <summary>
        /// Gets index of first non-equal char for two strings
        /// Not case sensitive.
        /// </summary>
        private static int GetMatchingCharsFromStart(string a, string b)
        {
            a = (a ?? string.Empty).ToLower();
            b = (b ?? string.Empty).ToLower();
            int matching = 0;
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                if (!Equals(a[i], b[i]))
                    break;

                matching++;
            }
            return matching;
        }

        public void Dispose()
        {
            MessageFilter.Revoke();
        }
    }
}
