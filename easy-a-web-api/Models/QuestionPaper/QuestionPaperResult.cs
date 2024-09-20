namespace easy_a_web_api.Models.QuestionPaper
{
    public class QuestionPaperResult
    {
        public string? Uid { get; set; }

        public string? QuestionPaperId { get; set; }

        public string? QuestionPaperName { get; set; }

        public string? QuestionPaperDueDate { get; set; }

        public string? QuestionPaperDescription { get; set; }

        public string? PDFLocation { get; set; }

        public int? NumQuestions { get; set; }
    }
}
