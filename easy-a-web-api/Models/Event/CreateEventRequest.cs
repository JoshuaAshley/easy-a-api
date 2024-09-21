namespace easy_a_web_api.Models.Event
{
    public class CreateEventRequest
    {
        public string? Uid { get; set; }

        public string? EventName { get; set; }

        public DateTime? EventDate { get; set; }
    }
}
