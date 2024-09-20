namespace easy_a_web_api.Models.Question
{
    public class CreateQuestionRequest
    {
        public string? Uid { get; set; }

        public string? QuestionPaperId { get; set; }

        public string? QuestionName { get; set; }

        public string? QuestionDescription { get; set; }

        public IFormFile? QuestionImage { get; set; }
    }
}
