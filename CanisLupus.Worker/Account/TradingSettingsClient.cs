using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using MongoDB.Driver;

namespace CanisLupus.Worker.Account
{
    public interface ITradingSettingsClient
    {
         Task<TradingSettings> GetAsync();
         Task<TradingSettings> InsertOrUpdateAsync(TradingSettings settings);
    }

    public class TradingSettingsClient : ITradingSettingsClient
    {
        public const string TradingSettingsCollectionName = "TradingSettings";
        private MongoDbClient dbClient;

        public TradingSettingsClient(MongoDbClient dbClient)
        {
            this.dbClient = dbClient;
        }

        public async Task<TradingSettings> InsertOrUpdateAsync(TradingSettings settings)
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.UserId == "dragos");

            var tradingSettings = (await collection.FindAsync(filter)).FirstOrDefault();

            if (tradingSettings != null)
            {
                var update = Builders<TradingSettings>.Update
                    .Set(m => m.UpdatedDate, DateTime.Now)
                    .Set(m => m.ProfitPercentage, settings.ProfitPercentage)
                    .Set(m => m.SpendLimit, settings.SpendLimit)
                    .Set(m => m.StopLossPercentage, settings.StopLossPercentage )
                    .Set(m => m.TotalSpendLimit, settings.TotalSpendLimit)
                    .Set(m => m.TradingStatus, settings.TradingStatus);

                var updatedSettings = await collection.UpdateOneAsync<TradingSettings>(filter, update);

                return settings;
            }

            var newSettings = new TradingSettings()
            {
                CreatedDate = DateTime.UtcNow,
                ProfitPercentage = settings.ProfitPercentage,
                SpendLimit = settings.SpendLimit,
                StopLossPercentage = settings.StopLossPercentage,
                TotalSpendLimit = settings.TotalSpendLimit,
            };

            await collection.InsertOneAsync(newSettings);
            return newSettings;
        }

        public async Task<TradingSettings> GetAsync()
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.UserId == "dragos");
            var tradingSettings = (await collection.FindAsync(filter)).FirstOrDefault();

            return tradingSettings;
        }
    }
}