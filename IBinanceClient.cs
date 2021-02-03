using System.Threading.Tasks;

namespace CanisLupus
{
    public interface IBinanceClient
    {
        Task<SymbolCandle> GetLatestCandleAsync(string pairName);
    }
}