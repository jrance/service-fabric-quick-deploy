using System;
using System.Diagnostics;
using System.Threading;
using CommandLine;
using ServiceFabricQuickDeploy.Logging;
using ServiceFabricQuickDeploy.Models;
using ServiceFabricQuickDeploy.Services;

namespace ServiceFabricQuickDeploy
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            try
            {
                var options = new Options();
                if (Parser.Default.ParseArguments(args, options))
                {
                    Thread.Sleep(10*100);
                    var stopwatch = Stopwatch.StartNew();

                    logger.LogInformation($"Begin deploy {(options.AttachDebugger ? "and attach" : "")} of Service Fabric services");
                    using (var vsEnvironment = new VsEnvironment(logger))
                    {
                        IServiceFabricAppDiscovery appDiscovery = new ServiceFabricAppDiscovery(vsEnvironment);
                        IQuickDeploy quickDeploy = new QuickDeploy(vsEnvironment, logger);
                        quickDeploy.DeployAsync(appDiscovery.GetServiceFabricAppDetails(), options.AttachDebugger).GetAwaiter().GetResult();
                    }
                    var elapsedSecs = Math.Round((double)stopwatch.ElapsedMilliseconds / 1000, 2);
                    logger.LogInformation($"Deploy {(options.AttachDebugger ? "and attach" : "")} completed in {elapsedSecs} seconds");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error occurred during quick debug", ex);
            }
        }
    }
}
