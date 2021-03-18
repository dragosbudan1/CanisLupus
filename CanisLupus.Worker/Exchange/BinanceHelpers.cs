using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using CanisLupus.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CanisLupus.Worker.Exchange
{
    public static class BinanceHelpers
    {
        public static CandleRawData MapResponseToSymbolCandle(string contentResponse)
        {
            var jarray = JArray.Parse(contentResponse);

            return new CandleRawData()
            {
                OpenTime = UnixTimeStampToDateTime(jarray[0][0].Value<long>()).GetValueOrDefault(),
                Open = Convert.ToDecimal(jarray[0][1].Value<string>()),
                High = Convert.ToDecimal(jarray[0][2].Value<string>()),
                Low = Convert.ToDecimal(jarray[0][3].Value<string>()),
                Close = Convert.ToDecimal(jarray[0][4].Value<string>()),
                Volume = jarray[0][5].Value<string>(),
                CloseTime = UnixTimeStampToDateTime(jarray[0][6].Value<long>()).GetValueOrDefault(),
                QuoteAssetVolume = jarray[0][7].Value<string>(),
                NumberOfTrades = jarray[0][8].Value<long>(),
            };
        }

        public static BinanceOrderResponse MapToBinanceOrderResponse(string content)
        {
            var response = JsonConvert.DeserializeObject<BinanceOrderResponse>(content);

            return response;
        }

        public static Order MapToOrder(this BinanceOrderResponse response, Order origOrder)
        {
            return new Order
            {
                Id = response.ClientOrderId,
                Price = response.Price,
                Quantity = response.OrigQty,
                ProfitPercentage = origOrder.ProfitPercentage,
                StopLossPercentage = origOrder.StopLossPercentage,
                Symbol = response.Symbol,
                SpendAmount = origOrder.SpendAmount,
                Status = MapToOrderStatus(response.Status),
                Side = MapToOrderType(response.Side),
                CreatedDate = UnixTimeStampToDateTime(response?.TransactTime) ?? origOrder.CreatedDate,
                UpdatedDate = UnixTimeStampToDateTime(response?.TransactTime) ?? origOrder.UpdatedDate
            };
        }

        public static OrderStatus MapToOrderStatus(string respStatus)
        {
            switch (respStatus)
            {
                case "CANCELED":
                    return OrderStatus.Cancelled;
                case "NEW":
                    return OrderStatus.New;
                case "PARTIALLY_FILLED":
                    return OrderStatus.Partial;
                case "FILLED":
                    return OrderStatus.Filled;
                case "REJECTED":
                    return OrderStatus.Rejected;
                case "EXPIRED":
                    return OrderStatus.Expired;
                default:
                    throw new ArgumentException($"{respStatus} is not valid");
            }
        }

        /*NEW	The order has been accepted by the engine.
        PARTIALLY_FILLED	A part of the order has been filled.
        FILLED	The order has been completed.
        CANCELED	The order has been canceled by the user.
        PENDING_CANCEL	Currently unused
        REJECTED	The order was not accepted by the engine and not processed.
        EXPIRED	The order was canceled according to the order type's rules (e.g. LIMIT FOK orders with no fill, LIMIT IOC or MARKET orders that partially fill) or by the exchange, (e.g. orders canceled during liquidation, orders canceled during maintenance)*/

        public static OrderSide MapToOrderType(string respType)
        {
            switch (respType)
            {
                case "SELL":
                    return OrderSide.Sell;
                case "BUY":
                default:
                    return OrderSide.Buy;
            }
        }

        public static List<BinanceOrderResponse> MapToListBinanceOrderResponse(string content)
        {
            var jarray = JArray.Parse(content);
            var list = new List<BinanceOrderResponse>();

            foreach (var item in jarray.Children())
            {
                var order = JsonConvert.DeserializeObject<BinanceOrderResponse>(item.ToString());
                list.Add(order);
            }

            return list;
        }
        public static List<CandleRawData> MapResponseToListSymbolCandle(string contentResponse)
        {
            var jarray = JArray.Parse(contentResponse);
            var list = new List<CandleRawData>();

            foreach (var item in jarray.Children())
            {
                var candle = new CandleRawData()
                {
                    OpenTime = UnixTimeStampToDateTime(item[0].Value<long>()).Value,
                    Open = Convert.ToDecimal(item[1].Value<string>()),
                    High = Convert.ToDecimal(item[2].Value<string>()),
                    Low = Convert.ToDecimal(item[3].Value<string>()),
                    Close = Convert.ToDecimal(item[4].Value<string>()),
                    Volume = item[5].Value<string>(),
                    CloseTime = UnixTimeStampToDateTime(item[6].Value<long>()).Value,
                    QuoteAssetVolume = item[7].Value<string>(),
                    NumberOfTrades = item[8].Value<long>(),
                };

                list.Add(candle);
            }

            return list;
        }

        public static ExchangeInfo MapResponseToListSymbolInfo(string contentResponse)
        {
            var exchangeInfo = JsonConvert.DeserializeObject<ExchangeInfo>(contentResponse);
            return exchangeInfo;
        }

        public static string GetTradeQueryString(string timestamp, string symbol = "BTCUSDT", OrderSide side = OrderSide.Sell, decimal? quantity = 0.01m, decimal? price = 46000m)
        {
            return new StringBuilder()
                .Append($"symbol={symbol}&")
                .Append($"side={side.ToString().ToUpper()}&")
                .Append("type=LIMIT&")
                .Append("timeInForce=GTC&")
                .Append($"quantity={quantity?.ToString()}&")
                .Append($"price={price?.ToString()}&")
                // .Append($"newClientOrderId=12345566655&")
                .Append($"recvWindow=50000&")
                .Append($"timestamp={timestamp}")
                .ToString();
        }


        public static string GetTradeQueryString(string timestamp, BinanceOrderRequest req)
        {
            return new StringBuilder()
                .Append($"symbol={req.Symbol}&")
                .Append($"side={req.Side.ToString().ToUpper()}&")
                .Append("type=LIMIT&")
                .Append("timeInForce=GTC&")
                .Append($"quantity={req.Quantity?.ToString()}&")
                .Append($"price={req.Price?.ToString()}&")
                .Append($"newClientOrderId={req.ClientOrderId}&")
                .Append($"recvWindow=50000&")
                .Append($"timestamp={timestamp}")
                .ToString();
        }

        public static string GetOpenOrderQueryString(string timestamp, string symbol = null)
        {
            return new StringBuilder()
                .Append($"symbol={symbol}&")
                .Append($"timestamp={timestamp}&")
                .Append("recvWindow=50000")
                .ToString();
        }

        public static string GetCancelOrderQueryString(string timestamp, string symbol, string orderId)
        {
            return new StringBuilder()
                .Append($"symbol={symbol}&")
                .Append($"origClientOrderId={orderId}&")
                .Append($"timestamp={timestamp}&")
                .Append("recvWindow=50000")
                .ToString();
        }

        public static string GenerateHMAC256(string text, string key)
        {
            var encoding = new UTF8Encoding();

            Byte[] textBytes = encoding.GetBytes(text);
            Byte[] keyBytes = encoding.GetBytes(key);

            Byte[] hashBytes;

            using (HMACSHA256 hash = new HMACSHA256(keyBytes))
                hashBytes = hash.ComputeHash(textBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        public static string GetUnixTimestamp()
        {
            long currentTimestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds;
            return currentTimestamp.ToString();
        }

        public static DateTime? UnixTimeStampToDateTime(long? unixTimeStamp)
        {
            if(unixTimeStamp == null)
            {
                return null;
            }
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp.Value).ToLocalTime();
            return dtDateTime;
        }
    }
}