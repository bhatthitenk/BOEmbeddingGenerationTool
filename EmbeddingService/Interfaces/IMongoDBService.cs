using MongoDB.Driver;

namespace BOEmbeddingService.Interfaces;

public interface IMongoDbService
{
    IMongoCollection<T> GetCollection<T>();

    Task InsertDocumentAsync<T>(T document);
}
