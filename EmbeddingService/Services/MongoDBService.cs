using BOEmbeddingService.Interfaces;
using MongoDB.Driver;

namespace BOEmbeddingService.Services;

public class MongoDbService : IMongoDbService
{
	private readonly IMongoDatabase _database;
	private string _collectionName;

	public MongoDbService(
		AppSettings appSettings)
	{
		var client = new MongoClient(appSettings.MongoDbSettings.ConnectionString);
		_database = client.GetDatabase(appSettings.MongoDbSettings.DatabaseName);
		_collectionName = appSettings.MongoDbSettings.CollectionName;
	}

	public IMongoCollection<T> GetCollection<T>()
	{
		return _database.GetCollection<T>(_collectionName);
	}

	/// <summary>
	/// Inserts new document in database
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="document">model to be inserted</param>
	/// <returns></returns>
	public async Task InsertDocumentAsync<T>(T document)
	{
		var collection = GetCollection<T>();
		await collection.InsertOneAsync(document);
	}
}
