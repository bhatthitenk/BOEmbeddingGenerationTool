using MongoDB.Driver;

namespace BOEmbeddingService.Interfaces
{
	public interface IMongoDbService
	{
		IMongoCollection<T> GetCollection<T>(string collectionName);

		Task InsertDocumentAsync<T>(string collectionName, T document);
	}
}
