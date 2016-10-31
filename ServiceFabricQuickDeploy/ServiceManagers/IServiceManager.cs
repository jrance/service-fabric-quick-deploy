using System.Collections.Generic;
using System.Fabric.Description;
using System.Threading.Tasks;
using ServiceFabricQuickDeploy.Models;

namespace ServiceFabricQuickDeploy.ServiceManagers
{
    public interface IServiceManager
    {
        Task<ServiceDescription> StopService(ServiceFabricProject serviceProject);

        Task<ICollection<string>> StartService(ServiceFabricProject serviceProject,
            ServiceDescription serviceDescription, int instanceCount);
    }
}
