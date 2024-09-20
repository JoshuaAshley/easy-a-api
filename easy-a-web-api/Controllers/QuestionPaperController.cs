using easy_a_web_api.Models.QuestionPaper;
using easy_a_web_api.Services;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace easy_a_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionPaperController : ControllerBase
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "easy-a-dbad0.appspot.com";

        public QuestionPaperController()
        {
            _firestoreDb = FireStoreService.DB!;
            _storageClient = FireStoreService.StorageClient!;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateQuestionPaper([FromForm] CreateQuestionPaperRequest request)
        {
            try
            {
                // Convert the question paper due date to UTC (if not already in UTC)
                DateTime? questionPaperDueDateUtc = request.QuestionPaperDueDate?.ToUniversalTime();

                // Prepare Firestore data without the PDF location initially
                var questionPaperData = new
                {
                    questionPaperName = request.QuestionPaperName ?? string.Empty,
                    questionPaperDueDate = questionPaperDueDateUtc,
                    questionPaperDescription = request.QuestionPaperDescription ?? string.Empty,
                    pdfLocation = string.Empty // Placeholder for the PDF location
                };

                // Store question paper record inside the user's collection
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(request.Uid);
                CollectionReference questionPapersCollection = userDocRef.Collection("questionPapers");
                DocumentReference newQuestionPaperDocRef = questionPapersCollection.Document(); // Generate new document ID
                string questionPaperId = newQuestionPaperDocRef.Id;

                // Save the document with the auto-generated ID
                await newQuestionPaperDocRef.SetAsync(questionPaperData);

                string pdfUrl = string.Empty;

                // Check if a PDF file is provided in the request
                if (request.PdfFile != null)
                {
                    // Folder structure: users/{Uid}/questionPapers/{QuestionPaperId}/PDF
                    string storageFolder = $"question-papers/{questionPaperId}/PDF";

                    // Upload the PDF to Firebase Storage
                    pdfUrl = await FileManagementService.UploadPdfToStorage(_storageClient, _bucketName, storageFolder, request.PdfFile);

                    // Update the question paper document with the PDF location
                    var updateData = new Dictionary<string, object>
                    {
                        { "pdfLocation", pdfUrl }
                    };
                    await newQuestionPaperDocRef.UpdateAsync(updateData);
                }

                // Prepare the result model
                var result = new QuestionPaperResult
                {
                    Uid = request.Uid,
                    QuestionPaperId = questionPaperId,
                    QuestionPaperName = request.QuestionPaperName,
                    QuestionPaperDueDate = questionPaperDueDateUtc?.ToString("yyyy-MM-dd"),
                    QuestionPaperDescription = request.QuestionPaperDescription,
                    PDFLocation = pdfUrl
                };

                return Ok(new { message = "Question paper created successfully", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }
    }
}