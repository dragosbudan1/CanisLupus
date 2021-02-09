using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace CanisLupus.Common.Database
{
    public interface IDbClient
    {
        Task<T> InsertAsync<T>(T item, string collectionName);
        IMongoCollection<T> GetCollection<T>(string collectionName);
    }
    public class MongoDbClient : IDbClient
    {

        public async Task<T> InsertAsync<T>(T item, string collectionName)
        {
            var collection = GetCollection<T>(collectionName);
            await collection.InsertOneAsync(item);
            return item;
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            var client = new MongoClient("mongodb+srv://canislupusdba:Dtk3sG33LnQPfkcI@canislupus.cnl7z.mongodb.net/canislupus?retryWrites=true&w=majority");
            var database = client.GetDatabase("CanisLupus");

            return database.GetCollection<T>(collectionName);
        }
    }
}
