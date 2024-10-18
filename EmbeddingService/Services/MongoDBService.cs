using BOEmbeddingService.Interfaces;
using BOEmbeddingService.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BOEmbeddingService.Services
{
	public class MongoDbService : IMongoDbService
	{
		private readonly IMongoDatabase _database;
		private string collectionName = "PromptCollection";
		//private readonly IAppSettings _appSettings;

		public MongoDbService(IAppSettings appSettings)
		{
			var client = new MongoClient(appSettings.mongoDbSettings.ConnectionString);
			_database = client.GetDatabase(appSettings.mongoDbSettings.DatabaseName);
		}

		public IMongoCollection<T> GetCollection<T>()
		{
			return _database.GetCollection<T>(collectionName);
		}

		public async Task InsertDocumentAsync<T>(T document)
		{
			var collection = GetCollection<T>();
			await collection.InsertOneAsync(document);
		}

		// Add other common CRUD operations as needed
	}
}
