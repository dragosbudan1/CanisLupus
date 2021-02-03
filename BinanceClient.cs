using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CanisLupus
{
    public class BinanceClient : IBinanceClient
    {
        private ILogger<BinanceClient> logger;

        public BinanceClient(ILogger<BinanceClient> logger)
        {
            this.logger = logger;
        }

        public async Task<SymbolCandle> GetLatestCandleAsync(string pairName)
        {
            try 
            {
                if(string.IsNullOrEmpty(pairName))
                    throw new ArgumentNullException(nameof(pairName));

                var client = new HttpClient();
                var response = await client.GetAsync($"https://api.binance.com/api/v3/klines?symbol={pairName}&interval=1m&limit=1");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var symbolCandle = MapResponseToSymbolCandle(content);

                logger.LogInformation("Binance GetLatestCandle {pairName} {info}", pairName, symbolCandle.ToLoggable());

                return symbolCandle;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error GetLatestCandle", new {pairName});
                return null;
            }
        }

        private SymbolCandle MapResponseToSymbolCandle(string contentResponse)
        {
            var jarray = JArray.Parse(contentResponse);

            return new SymbolCandle()
            {
                OpenTime = jarray[0][0].Value<long>(),
                Open = jarray[0][1].Value<string>(),
                High = jarray[0][2].Value<string>(),
                Low = jarray[0][3].Value<string>(),
                Close = jarray[0][4].Value<string>(),
                Volume = jarray[0][5].Value<string>(),
                CloseTime = jarray[0][6].Value<long>(),
                QuoteAssetVolume = jarray[0][7].Value<string>(),
                NumberOfTrades = jarray[0][8].Value<long>(),
            };
        }

        //     [
        //   [
        //     1499040000000,      // Open time
        //     "0.01634790",       // Open
        //     "0.80000000",       // High
        //     "0.01575800",       // Low
        //     "0.01577100",       // Close
        //     "148976.11427815",  // Volume
        //     1499644799999,      // Close time
        //     "2434.19055334",    // Quote asset volume
        //     308,                // Number of trades
        //     "1756.87402397",    // Taker buy base asset volume
        //     "28.46694368",      // Taker buy quote asset volume
        //     "17928899.62484339" // Ignore.
        //   ]
        // ]

    }
}