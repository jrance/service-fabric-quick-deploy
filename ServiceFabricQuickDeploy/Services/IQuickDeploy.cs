using System.Threading.Tasks;
using ServiceFabricQuickDeploy.Models;

namespace ServiceFabricQuickDeploy.Services
{
    internal interface IQuickDeploy
    {
        Task DeployAsync(ServiceFabricApp appDetails, string serviceFabricAppPath, bool attachDebugger);
    }
}