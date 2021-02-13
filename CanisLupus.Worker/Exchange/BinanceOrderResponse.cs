namespace CanisLupus.Worker.Exchange
{
    public class BinanceOrderResponse
    {
        public string Symbol { get; set; }
        public long OrderId { get; set; }
        public long OrderListId { get; set; }
        public string ClientOrderId { get; set; }
        public string OrigClientOrderId { get; set; }
        public long? TransactTime { get; set; }
        public long? UpdateTime { get; set; }
        public decimal Price { get; set; }
        public decimal OrigQty { get; set; }
        public decimal ExecutedQty { get; set; }
        public decimal CummulativeQuotedQty { get; set; }
        public string Status { get; set; }
        public string TimeInForce { get; set; }
        public string Type { get; set; }
        public string Side { get; set; }
        public bool? IsWorking { get; set; }
    }

    /*\"symbol\":\"TRXBNB\",
    \"orderId\":3,
    \"orderListId\":-1,
    \"clientOrderId\":\"12345566655\",
    \"price\":\"0.00100000\",
    \"origQty\":\"100.00000000\",
    \"executedQty\":\"0.00000000\",
    \"cummulativeQuoteQty\":\"0.00000000\",
    \"status\":\"NEW\",
    \"timeInForce\":\"GTC\",
    \"type\":\"LIMIT\",
    \"side\":\"BUY\",
    \"stopPrice\":\"0.00000000\",
    \"icebergQty\":\"0.00000000\",
    \"time\":1613219474854,
    \"updateTime\":1613219474854,
    \"isWorking\":true,
    \"origQuoteOrderQty\":\"0.00000000\"}
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
        //\"fills\":[]}"*/

}