using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Services.Interfaces.Logging;
using Microsoft.Extensions.Logging;

namespace Persistence.Services
{
    public class SerilogLoggerAdapter<T>(ILogger<T> logger) : IAppLogger<T>
    {
        public void LogError(string message, Exception ex)
        {
            logger.LogError(ex, message);
        }

        public void LogInformation(string message)
        {
            logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            logger.LogWarning(message);
        }
    }
}