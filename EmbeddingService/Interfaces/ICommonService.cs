using BOEmbeddingService.Models;

namespace BOEmbeddingService.Interfaces;

public interface ICommonService
{
    Task<string[]> GetFiles(string path);
    Task WriteToFileAndDB(WriteToFileModel model);

}
