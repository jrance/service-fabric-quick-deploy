﻿using System;

namespace ServiceFabricQuickDeploy.Logging
{
    public interface ILogger
    {
        void LogInformation(string message);
        void LogError(string message, Exception ex);
    }
}