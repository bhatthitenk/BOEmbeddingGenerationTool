using BOEmbeddingService.Interfaces;
using Serilog;
using Serilog.Core;

namespace BOEmbeddingService.Services;

public class LoggerService : ILoggerService
{
    public Logger Logger { get; set; }
    public void GetInstance()
    {
        Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("Logs/embeddingLog.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
