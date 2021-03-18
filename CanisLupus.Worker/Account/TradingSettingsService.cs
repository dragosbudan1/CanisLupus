using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using CanisLupus.Worker.Exchange;
using MongoDB.Driver;
using NLog;

namespace CanisLupus.Worker.Account
{
    public interface ITradingSettingsService
    {
         Task<TradingSettings> GetAsync(string symbol);
         Task<List<TradingSettings>> GetAllAsync();
         Task<TradingSettings> InsertOrUpdateAsync(TradingSettings settings);
         Task<bool> DeleteAsync(string symbol);
    }

    public class TradingSettingsService : ITradingSettingsService
    {
        public const string TradingSettingsCollectionName = "TradingSettings";
        private readonly IDbClient dbClient;
        private readonly IBinanceClient binanceClient;
        private readonly ILogger logger;

        public TradingSettingsService(IDbClient dbClient, IBinanceClient binanceClient)
        {
            this.dbClient = dbClient;
            this.binanceClient = binanceClient;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        public async Task<TradingSettings> InsertOrUpdateAsync(TradingSettings settings)
        {
            if(string.IsNullOrEmpty(settings?.Symbol) || !(await binanceClient.ValidateSymbolInfo(settings.Symbol)))
            {
                logger.Error($"Symbol is not valid {settings?.Symbol}");
                return null;
            }
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.Symbol == settings.Symbol);

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

            settings.CreatedDate = DateTime.UtcNow;

            await collection.InsertOneAsync(settings);
            return settings;
        }

        public async Task<TradingSettings> GetAsync(string symbol)
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.Symbol == symbol);
            var tradingSettings = (await collection.FindAsync(filter)).FirstOrDefault();

            return tradingSettings;
        }

        public async Task<List<TradingSettings>> GetAllAsync()
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsCollectionName);
            return (await collection.FindAsync<TradingSettings>(Builders<TradingSettings>.Filter.Empty)).ToList();
        }

        public async Task<bool> DeleteAsync(string symbol)
        {
            var collection = dbClient.GetCollection<TradingSettings>(TradingSettingsCollectionName);
            Expression<Func<TradingSettings, bool>> filter = m => (m.Symbol == symbol);
            var result = (await collection.DeleteOneAsync(filter));
            return result.DeletedCount == 1;
        }
    }
}