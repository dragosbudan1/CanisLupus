using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CanisLupus.Common.Database;
using CanisLupus.Common.Models;
using MongoDB.Driver;

namespace CanisLupus.Worker.Trader
{
    public interface IWalletClient
    {
        Task<Wallet> CreateWallet(string id);
        Task<string> UpdateWallet(string id, decimal amount);
    }

    public class WalletClient : IWalletClient
    {
        private readonly IDbClient dbClient;

        public WalletClient(IDbClient dbClient)
        {
            this.dbClient = dbClient;
        }

        public const string WalletColectionName = "Wallets";

        public async Task<Wallet> CreateWallet(string id)
        {
            var collection = dbClient.GetCollection<Wallet>(WalletColectionName);
            Expression<Func<Wallet, bool>> filter = m => (m.Id == id);
            var update = Builders<Wallet>.Update
                .Set(m => m.CreatedDate, DateTime.Now)
                .Set(m => m.Amount, 0.0m)
                .Set(m => m.Id, id);


            var result = await collection.FindOneAndUpdateAsync<Wallet>(filter, update);
            return result;
        }

        public async Task<string> UpdateWallet(string id, decimal amount)
        {
            var collection = dbClient.GetCollection<Wallet>(WalletColectionName);
            Expression<Func<Wallet, bool>> filter = m => (m.Id == id);

            var wallet = (await collection.FindAsync(filter)).FirstOrDefault();

            if (wallet != null)
            {
                var update = Builders<Wallet>.Update
                    .Set(m => m.UpdateDate, DateTime.Now)
                    .Set(m => m.Amount, wallet.Amount + amount);

                var updatedWallet = await collection.UpdateOneAsync<Wallet>(filter, update);

                return id;
            }

            var newWallet = new Wallet()
            {
                CreatedDate = DateTime.UtcNow,
                Amount = amount,
                Id = id
            };

            await collection.InsertOneAsync(newWallet);
            return id;
        }
    }
}