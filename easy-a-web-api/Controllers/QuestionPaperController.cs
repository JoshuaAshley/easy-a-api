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
                DateTime? questionPaperDueDateUtc = request.QuestionPaperDueDate.HasValue
                    ? DateTime.SpecifyKind(request.QuestionPaperDueDate.Value, DateTimeKind.Unspecified)
                          .ToUniversalTime()
                    : null;

                // Prepare Firestore data without the PDF location initially
                var questionPaperData = new
                {
                    questionPaperName = request.QuestionPaperName ?? string.Empty,
                    questionPaperDueDate = questionPaperDueDateUtc,
                    questionPaperDescription = request.QuestionPaperDescription ?? string.Empty,
                    pdfLocation = string.Empty
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
                    var updatePDFData = new Dictionary<string, object>
                    {
                        { "pdfLocation", pdfUrl }
                    };
                    await newQuestionPaperDocRef.UpdateAsync(updatePDFData);
                }

                int numQuestions = 0;
                var updateData = new Dictionary<string, object>
                {
                    { "numQuestions", numQuestions }
                };

                await newQuestionPaperDocRef.UpdateAsync(updateData);

                // Convert the due date to UTC+2
                string? questionPaperDueDateUtcPlus2 = questionPaperDueDateUtc.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(questionPaperDueDateUtc.Value, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"))
                                     .ToString("yyyy-MM-dd")
                    : null;

                // Prepare the result model with the converted due date (UTC+2)
                var result = new QuestionPaperResult
                {
                    Uid = request.Uid,
                    QuestionPaperId = questionPaperId,
                    QuestionPaperName = request.QuestionPaperName,
                    QuestionPaperDueDate = questionPaperDueDateUtcPlus2,
                    QuestionPaperDescription = request.QuestionPaperDescription,
                    PDFLocation = pdfUrl,
                    NumQuestions = numQuestions
                };

                return Ok(new { message = "Question paper created successfully", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("list/{uid}")]
        public async Task<IActionResult> GetQuestionPapersByUser(string uid)
        {
            try
            {
                // Reference to the user's question papers collection
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);
                CollectionReference questionPapersCollection = userDocRef.Collection("questionPapers");

                // Get all documents in the question papers collection
                QuerySnapshot questionPapersSnapshot = await questionPapersCollection.GetSnapshotAsync();

                // Define the desired timezone (UTC+2 in this case)
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"); // Use your specific timezone

                var questionPapersList = questionPapersSnapshot.Documents.Select(doc => new QuestionPaperResult
                {
                    Uid = uid,
                    QuestionPaperId = doc.Id,
                    QuestionPaperName = doc.ContainsField("questionPaperName") ? doc.GetValue<string>("questionPaperName") : "",
                    QuestionPaperDueDate = doc.ContainsField("questionPaperDueDate") ?
                        TimeZoneInfo.ConvertTimeFromUtc(doc.GetValue<DateTime>("questionPaperDueDate"), timeZone).ToString("yyyy-MM-dd") : "",
                    QuestionPaperDescription = doc.ContainsField("questionPaperDescription") ? doc.GetValue<string>("questionPaperDescription") : "",
                    PDFLocation = doc.ContainsField("pdfLocation") ? doc.GetValue<string>("pdfLocation") : "",
                    NumQuestions = doc.ContainsField("numQuestions") ? doc.GetValue<int>("numQuestions") : 0
                }).ToList();

                return Ok(new { questionPapers = questionPapersList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("{uid}/question-paper/{questionPaperId}")]
        public async Task<IActionResult> GetQuestionPaperById(string uid, string questionPaperId)
        {
            try
            {
                // Reference to the specific question paper document
                DocumentReference questionPaperDocRef = _firestoreDb.Collection("users").Document(uid)
                    .Collection("questionPapers").Document(questionPaperId);

                // Get the document
                DocumentSnapshot questionPaperSnapshot = await questionPaperDocRef.GetSnapshotAsync();

                if (!questionPaperSnapshot.Exists)
                {
                    return NotFound(new { error = "Question paper not found" });
                }

                // Define the desired timezone (UTC+2 in this case)
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

                // Prepare the result
                var questionPaperResult = new QuestionPaperResult
                {
                    Uid = uid,
                    QuestionPaperId = questionPaperSnapshot.Id,
                    QuestionPaperName = questionPaperSnapshot.ContainsField("questionPaperName") ? questionPaperSnapshot.GetValue<string>("questionPaperName") : "",
                    QuestionPaperDueDate = questionPaperSnapshot.ContainsField("questionPaperDueDate") ?
                        TimeZoneInfo.ConvertTimeFromUtc(questionPaperSnapshot.GetValue<DateTime>("questionPaperDueDate"), timeZone).ToString("yyyy-MM-dd") : "",
                    QuestionPaperDescription = questionPaperSnapshot.ContainsField("questionPaperDescription") ? questionPaperSnapshot.GetValue<string>("questionPaperDescription") : "",
                    PDFLocation = questionPaperSnapshot.ContainsField("pdfLocation") ? questionPaperSnapshot.GetValue<string>("pdfLocation") : "",
                    NumQuestions = questionPaperSnapshot.ContainsField("numQuestions") ? questionPaperSnapshot.GetValue<int>("numQuestions") : 0
                };

                return Ok(questionPaperResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }


    }
}