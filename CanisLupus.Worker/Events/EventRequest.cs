namespace CanisLupus.Worker.Events
{
    public class EventRequest
    {
        public string QueueName { get; set; }
        public string Value { get; set; }
    }
}