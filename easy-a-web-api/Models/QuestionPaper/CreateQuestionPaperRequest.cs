namespace easy_a_web_api.Models.QuestionPaper
{
    public class CreateQuestionPaperRequest
    {
        public string? Uid { get; set; }

        public string? QuestionPaperName { get; set; }

        public DateTime? QuestionPaperDueDate { get; set; }

        public string? QuestionPaperDescription { get; set; }

        public IFormFile? PdfFile { get; set; }
    }
}
