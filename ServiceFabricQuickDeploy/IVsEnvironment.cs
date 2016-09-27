using EnvDTE;

namespace ServiceFabricQuickDeploy
{
    public interface IVsEnvironment
    {
        Solution GetSolution();
        void DetachDebugger(string processName);
        void AttachDebugger(string processName);
    }
}