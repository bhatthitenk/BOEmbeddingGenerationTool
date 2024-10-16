using Serilog.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOEmbeddingService.Interfaces;

namespace BOEmbeddingService.Services
{
    public class LoggerService : ILoggerService
    {
        public Logger _logger {  get; set; }
        public void GetInstance()
        {
            _logger = new LoggerConfiguration()
                            .WriteTo.Console()
                            .WriteTo.File("Logs/embeddingLog.txt", rollingInterval: RollingInterval.Day)
                            .CreateLogger();
        }
    }
}
