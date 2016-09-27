namespace ServiceFabricQuickDeploy
{
    public class QuickDeployOrchestrator
    {
        private QuickDeploy _quickDeploy;
        private VsEnvironment _vsEnvironment;

        public QuickDeployOrchestrator(QuickDeploy quickDeploy, VsEnvironment vsEnvironment)
        {
            _quickDeploy = quickDeploy;
            _vsEnvironment = vsEnvironment;
        }
        public void Deloy(bool attach)
        {
        }
    }
}
