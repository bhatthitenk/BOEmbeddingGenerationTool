using Serilog.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService
{
    public static class LoggerService
    {
        public static Logger GetInstance()
        {
            return new LoggerConfiguration()
                            .WriteTo.Console()
                            .WriteTo.File("Logs/embeddingLog.txt", rollingInterval: RollingInterval.Day)
                            .CreateLogger();
        }
    }
}
