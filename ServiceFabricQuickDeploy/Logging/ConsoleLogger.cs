using System;

namespace ServiceFabricQuickDeploy.Logging
{
    internal class ConsoleLogger : ILogger
    {
        public void LogInformation(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string message, Exception ex)
        {
            Console.WriteLine(message);
            Console.WriteLine(ex);
        }
    }
}