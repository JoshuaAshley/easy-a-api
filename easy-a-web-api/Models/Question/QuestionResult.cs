namespace easy_a_web_api.Models.Question
{
    public class QuestionResult
    {
        public string? Uid { get; set; }

        public string? QuestionPaperId { get; set; }

        public string? QuestionId { get; set; }

        public string? QuestionName { get; set; }

        public string? QuestionDescription { get; set; }

        public string? ImageLocation { get; set; }

        public int? TotatLoggedTime { get; set; }

        public bool? IsCompleted { get; set; }
    }
}
