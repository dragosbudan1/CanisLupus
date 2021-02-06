using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CanisLupus.Worker.Extensions;
using CanisLupus.Worker.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CanisLupus.Worker.Exchange
{
   public interface IBinanceClient
    {
        Task<CandleRawData> GetLatestCandleAsync(string pairName);
        Task<List<CandleRawData>> GetCandlesAsync(string pairName, int count, string interval);
    }

    public class BinanceClient : IBinanceClient
    {
        private ILogger<BinanceClient> logger;

        public BinanceClient(ILogger<BinanceClient> logger)
        {
            this.logger = logger;
        }

        public async Task<List<CandleRawData>> GetCandlesAsync(string pairName, int count, string interval)
        {
            try
            {
                if (string.IsNullOrEmpty(pairName))
                    throw new ArgumentNullException(nameof(pairName));

                var client = new HttpClient();
                var response = await client.GetAsync($"https://api.binance.com/api/v3/klines?symbol={pairName}&interval={interval}&limit={count}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var listSymbolCandle = MapResponseToListSymbolCandle(content);

                logger.LogInformation("Binance GetCandlesAsync {pairName} {count} {interval}", pairName, count, interval);

                return listSymbolCandle;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error GetLatestCandle", new { pairName });
                return null;
            }
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public async Task<CandleRawData> GetLatestCandleAsync(string pairName)
        {
            try
            {
                if (string.IsNullOrEmpty(pairName))
                    throw new ArgumentNullException(nameof(pairName));

                var client = new HttpClient();
                var response = await client.GetAsync($"https://api.binance.com/api/v3/klines?symbol={pairName}&interval=1m&limit=1");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var symbolCandle = MapResponseToSymbolCandle(content);

                logger.LogInformation("Binance GetLatestCandle {pairName} {info}", pairName, symbolCandle.ToLoggable());

                return symbolCandle;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error GetLatestCandle", new { pairName });
                return null;
            }
        }

        private CandleRawData MapResponseToSymbolCandle(string contentResponse)
        {
            var jarray = JArray.Parse(contentResponse);

            return new CandleRawData()
            {
                OpenTime = UnixTimeStampToDateTime(jarray[0][0].Value<long>()),
                Open = Convert.ToDouble(jarray[0][1].Value<string>()),
                High = Convert.ToDouble(jarray[0][2].Value<string>()),
                Low = Convert.ToDouble(jarray[0][3].Value<string>()),
                Close = Convert.ToDouble(jarray[0][4].Value<string>()),
                Volume = jarray[0][5].Value<string>(),
                CloseTime = UnixTimeStampToDateTime(jarray[0][6].Value<long>()),
                QuoteAssetVolume = jarray[0][7].Value<string>(),
                NumberOfTrades = jarray[0][8].Value<long>(),
            };
        }

        private List<CandleRawData> MapResponseToListSymbolCandle(string contentResponse)
        {
            var jarray = JArray.Parse(contentResponse);
            var list = new List<CandleRawData>();

            foreach (var item in jarray.Children())
            {
                var candle = new CandleRawData()
                {
                    OpenTime = UnixTimeStampToDateTime(item[0].Value<long>()),
                    Open = Convert.ToDouble(item[1].Value<string>()),
                    High = Convert.ToDouble(item[2].Value<string>()),
                    Low = Convert.ToDouble(item[3].Value<string>()),
                    Close = Convert.ToDouble(item[4].Value<string>()),
                    Volume = item[5].Value<string>(),
                    CloseTime = UnixTimeStampToDateTime(item[6].Value<long>()),
                    QuoteAssetVolume = item[7].Value<string>(),
                    NumberOfTrades = item[8].Value<long>(),
                };

                list.Add(candle);
            }

            return list;
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