using ServiceFabricQuickDeploy.Models;

namespace ServiceFabricQuickDeploy.Services
{
    public interface IServiceFabricAppDiscovery
    {
        ServiceFabricApp GetServiceFabricAppDetails();
    }
}