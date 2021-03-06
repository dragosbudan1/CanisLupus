﻿using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NLog;

namespace CanisLupus.Common.Database
{
    public interface IDbClient
    {
        Task<T> InsertAsync<T>(T item, string collectionName);
        IMongoCollection<T> GetCollection<T>(string collectionName);
    }
    public class MongoDbClient : IDbClient
    {
        private readonly DbSettings dbSettings;
        private readonly ILogger logger;

        public MongoDbClient(IOptions<DbSettings> dbSettings)
        {
            this.dbSettings = dbSettings.Value;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<T> InsertAsync<T>(T item, string collectionName)
        {
            var collection = GetCollection<T>(collectionName);
            await collection.InsertOneAsync(item);
            return item;
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            try
            {
                var uri = string.Format(dbSettings.URI, dbSettings.User, dbSettings.Password, dbSettings.DbName);
                var client = new MongoClient(uri);
                var database = client.GetDatabase(dbSettings.DbName);

                return database.GetCollection<T>($"{collectionName}.{dbSettings.Environment}");
            }
            catch (System.Exception ex)
            {
                logger?.Fatal(ex, ex.Message);
                return null;
            }

        }
    }
}
