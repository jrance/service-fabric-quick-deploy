using EnvDTE90;
using ServiceFabricQuickDeploy.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFabricQuickDeploy
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var attach = true;
            var output = attach ?
                $"Begin deploy and attach for Service Fabric services" :
                $"Begin deploy of Service Fabric services";

            Console.WriteLine(output);

            var stopwatch = Stopwatch.StartNew();
            
            //System.Threading.Thread.Sleep(1000 * 10);

            var vsEnvironment = new VsEnvironment();
            var quickDeploy = new QuickDeploy(vsEnvironment);
            var serviceFabricServiceDiscovery = new ServiceFabricAppDiscovery(vsEnvironment);
            var appDetails = serviceFabricServiceDiscovery.GetServiceFabricAppDetails();
            quickDeploy.Deploy(appDetails, true);

            var elapsedSeconds = Math.Round((double)stopwatch.ElapsedMilliseconds / 1000, 2);

            output = attach ? 
                $"Deploy and attach completed in {elapsedSeconds} seconds" : 
                $"Deploy completed in {elapsedSeconds} seconds";

            Console.WriteLine(output);
        }
    }
}
