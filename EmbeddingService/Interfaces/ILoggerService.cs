using Serilog.Core;

namespace BOEmbeddingService.Interfaces;

public interface ILoggerService
{
    public Logger Logger { get; set; }
    void GetInstance();
}
