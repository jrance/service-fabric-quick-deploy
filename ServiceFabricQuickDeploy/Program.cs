using System;
using System.Diagnostics;
using CommandLine;
using ServiceFabricQuickDeploy.Logging;
using ServiceFabricQuickDeploy.Models;
using ServiceFabricQuickDeploy.ServiceManagers;
using ServiceFabricQuickDeploy.Services;

namespace ServiceFabricQuickDeploy
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            try
            {
                var options = new Options();
                if (Parser.Default.ParseArguments(args, options))
                {
                    var stopwatch = Stopwatch.StartNew();

                    logger.LogInformation(
                        $"Begin deploy {(options.AttachDebugger ? "and attach" : "")} of Service Fabric services");
                    using (var vsEnvironment = new VsEnvironment(logger))
                    {
                        IProcessService processService = new ProcessService();
                        IServiceFabricAppDiscovery appDiscovery = new ServiceFabricAppDiscovery(vsEnvironment);
                        IServiceManager serviceManager = options.KillProcesses
                            ? (IServiceManager) new KillProcessServiceManager(processService)
                            : new ServiceFabricApiServiceManager(processService);
                        IQuickDeploy quickDeploy = new QuickDeploy(vsEnvironment, serviceManager, processService, logger);

                        var appDetails = appDiscovery.GetServiceFabricAppDetails();
                        quickDeploy.DeployAsync(appDetails, options.AttachDebugger).GetAwaiter().GetResult();
                    }
                    var elapsedSecs = Math.Round((double) stopwatch.ElapsedMilliseconds/1000, 2);
                    logger.LogInformation(
                        $"Deploy {(options.AttachDebugger ? "and attach" : "")} completed in {elapsedSecs} seconds");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error occurred during quick debug", ex);
            }
        }
    }
}
