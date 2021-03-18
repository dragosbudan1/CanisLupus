using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CanisLupus.Worker.Extensions;
using CanisLupus.Common.Models;
using NLog;
using Microsoft.Extensions.Options;
using System.Linq;

namespace CanisLupus.Worker.Exchange
{
    public interface IBinanceClient
    {
        Task<CandleRawData> GetLatestCandleAsync(string pairName);
        Task<List<CandleRawData>> GetCandlesAsync(string pairName, int count, string interval);
        Task<BinanceOrderResponse> CreateOrder(BinanceOrderRequest request);
        Task<List<BinanceOrderResponse>> GetOpenOrders(string symbol);
        Task<List<BinanceOrderResponse>> CancelAllOrders(string symbol);
        Task<BinanceOrderResponse> CancelOrder(string symbol, string orderId);
        Task<BinanceOrderResponse> GetOrder(string symbol, string clientOrderId);
        Task<bool> ValidateSymbolInfo(string symbol);
    }

    public class BinanceClient : IBinanceClient
    {
        private readonly ILogger logger;
        private readonly BinanceSettings settings;

        public BinanceClient(IOptions<BinanceSettings> settings)
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.settings = settings.Value;
        }

        public async Task<List<CandleRawData>> GetCandlesAsync(string pairName, int count, string interval)
        {
            try
            {
                if (string.IsNullOrEmpty(pairName))
                    throw new ArgumentNullException(nameof(pairName));

                var client = new HttpClient();
                var response = await client.GetAsync($"{settings.Url}/klines?symbol={pairName}&interval={interval}&limit={count}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var listSymbolCandle = BinanceHelpers.MapResponseToListSymbolCandle(content);

                logger.Info("Binance GetCandlesAsync {pairName} {count} {interval}", pairName, count, interval);

                return listSymbolCandle;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error GetLatestCandle", new { pairName });
                return null;
            }
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
                var symbolCandle = BinanceHelpers.MapResponseToSymbolCandle(content);

                logger.Info("Binance GetLatestCandle {pairName} {info}", pairName, symbolCandle.ToLoggable());

                return symbolCandle;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error GetLatestCandle", new { pairName });
                return null;
            }
        }

        public async Task<List<BinanceOrderResponse>> GetOpenOrders(string symbol)
        {
            try
            {
                var url = $"{settings.Url}/openOrders";
                var timestamp = BinanceHelpers.GetUnixTimestamp();
                var queryString = BinanceHelpers.GetOpenOrderQueryString(timestamp, symbol);
                var hmac = BinanceHelpers.GenerateHMAC256(queryString, settings.SecretKey);
                var requestUri = $"{url}?{queryString}&signature={hmac}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                httpRequest.Headers.Add("X-MBX-APIKEY", settings.ApiKey);

                using var client = new HttpClient();
                var response = await client.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = BinanceHelpers.MapToListBinanceOrderResponse(content);
                logger.Error($"Log Orders: {content}");

                return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "Error get open orders");
                return null;
            }
        }

        public async Task<BinanceOrderResponse> CreateOrder(BinanceOrderRequest request)
        {
            try
            {
                var url = $"{settings.Url}/order";
                var timestamp = BinanceHelpers.GetUnixTimestamp();
                var queryString = BinanceHelpers.GetTradeQueryString(timestamp, request);
                var hmac = BinanceHelpers.GenerateHMAC256(queryString, settings.SecretKey);
                var requestUri = $"{url}?{queryString}&signature={hmac}";

                HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
                httpRequest.Headers.Add("X-MBX-APIKEY", settings.ApiKey);

                using var client = new HttpClient();
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();
                logger.Info($"Consntnet: {content}");
                response.EnsureSuccessStatusCode();
                if(content == null)
                {
                    logger.Error($"Error trade, no content {response}");
                    return null;
                }
                
                var binanceOrderResponse = BinanceHelpers.MapToBinanceOrderResponse(content);
                return binanceOrderResponse;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error trade {ex.Message}");
                return null;
            }
        }

        public async Task<List<BinanceOrderResponse>> CancelAllOrders(string symbol)
        {
            try
            {
                var url = $"{settings.Url}/openOrders";
                var timestamp = BinanceHelpers.GetUnixTimestamp();
                var queryString = BinanceHelpers.GetOpenOrderQueryString(timestamp, symbol);
                var hmac = BinanceHelpers.GenerateHMAC256(queryString, settings.SecretKey);
                var requestUri = $"{url}?{queryString}&signature={hmac}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, requestUri);
                httpRequest.Headers.Add("X-MBX-APIKEY", settings.ApiKey);

                using var client = new HttpClient();
                var response = await client.SendAsync(httpRequest);

                var content = await response.Content.ReadAsStringAsync();
                var result = BinanceHelpers.MapToListBinanceOrderResponse(content);
                logger.Error($"Log Orders: {content}");

                return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "Error cancel open orders");
                return null;
            }
        }

        public async Task<BinanceOrderResponse> CancelOrder(string symbol, string orderId)
        {
            try
            {
                var url = $"{settings.Url}/order";
                var timestamp = BinanceHelpers.GetUnixTimestamp();
                var queryString = BinanceHelpers.GetCancelOrderQueryString(timestamp, symbol, orderId);
                var hmac = BinanceHelpers.GenerateHMAC256(queryString, settings.SecretKey);
                var requestUri = $"{url}?{queryString}&signature={hmac}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, requestUri);
                httpRequest.Headers.Add("X-MBX-APIKEY", settings.ApiKey);

                using var client = new HttpClient();
                var response = await client.SendAsync(httpRequest);

                var content = await response.Content.ReadAsStringAsync();
                var result = BinanceHelpers.MapToBinanceOrderResponse(content);
                logger.Error($"Log Orders: {content}");

                return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "Error cancel open order");
                return null;
            }
        }

        public async Task<BinanceOrderResponse> GetOrder(string symbol, string clientOrderId)
        {
            try
            {
                var url = $"{settings.Url}/order";
                var timestamp = BinanceHelpers.GetUnixTimestamp();
                var queryString = BinanceHelpers.GetCancelOrderQueryString(timestamp, symbol, clientOrderId);
                var hmac = BinanceHelpers.GenerateHMAC256(queryString, settings.SecretKey);
                var requestUri = $"{url}?{queryString}&signature={hmac}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
                httpRequest.Headers.Add("X-MBX-APIKEY", settings.ApiKey);

                using var client = new HttpClient();
                var response = await client.SendAsync(httpRequest);

                var content = await response.Content.ReadAsStringAsync();
                var result = BinanceHelpers.MapToBinanceOrderResponse(content);
                logger.Error($"Log Orders: {content}");

                return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex, "Error get order");
                return null;
            }
        }

        public async Task<bool> ValidateSymbolInfo(string pairName)
        {
            try
            {
                if (string.IsNullOrEmpty(pairName))
                    throw new ArgumentNullException(nameof(pairName));

                var client = new HttpClient();
                var response = await client.GetAsync($"{settings.Url}/exchangeInfo");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var exchangeInfo = BinanceHelpers.MapResponseToListSymbolInfo(content);

                return exchangeInfo.Symbols.Any(x => x.Symbol == pairName && x.IsSpotTradingAllowed == true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error validating pair name", new { pairName });
                return false;
            }
        }

        //"{\"symbol\":\"TRXBNB\",
        //\"orderId\":5,\
        //"orderListId\":-1,\
        //"clientOrderId\":\"kUivlbbRlR1UeZOfnqQYy1\",
        //\"transactTime\":1613219760762,
        //\"price\":\"0.00100000\",
        //\"origQty\":\"100.00000000\",
        //\"executedQty\":\"0.00000000\",//
        //\"cummulativeQuoteQty\":\"0.00000000\",
        //\"status\":\"NEW\",
        //\"timeInForce\":\"GTC\",
        //\"type\":\"LIMIT\",
        //\"side\":\"BUY\",
        //\"fills\":[]}"



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