using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BOEmbeddingService.Services
{
	public class MongoDbService : IMongoDbService
	{
		private readonly IMongoDatabase _database;

		public MongoDbService(IOptions<MongoDbSettings> settings)
		{
			var client = new MongoClient(settings.Value.ConnectionString);
			_database = client.GetDatabase(settings.Value.DatabaseName);
		}

		public IMongoCollection<T> GetCollection<T>(string collectionName)
		{
			return _database.GetCollection<T>(collectionName);
		}

		public async Task InsertDocumentAsync<T>(string collectionName, T document)
		{
			var collection = GetCollection<T>(collectionName);
			await collection.InsertOneAsync(document);
		}

		// Add other common CRUD operations as needed
	}
}
