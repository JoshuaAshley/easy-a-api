using easy_a_web_api.Models.Question;
using easy_a_web_api.Services;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace easy_a_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionController : ControllerBase
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "easy-a-dbad0.appspot.com";

        public QuestionController()
        {
            _firestoreDb = FireStoreService.DB!;
            _storageClient = FireStoreService.StorageClient!;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateQuestion([FromForm] CreateQuestionRequest request)
        {
            try
            {
                // Prepare Firestore data without the PDF location initially
                var questionData = new
                {
                    questionName = request.QuestionName ?? string.Empty,
                    questionDescription = request.QuestionDescription,
                    imageLocation = string.Empty
                };

                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(request.Uid);

                // Reference to the specific question paper document where the question will be stored
                DocumentReference questionPaperDocRef = userDocRef.Collection("questionPapers").Document(request.QuestionPaperId);

                // Now, place the 'questions' collection inside the 'questionPapers' document
                CollectionReference questionsCollection = questionPaperDocRef.Collection("questions");

                // Generate a new document ID for the question inside the 'questions' collection
                DocumentReference newQuestionDocRef = questionsCollection.Document(); // Auto-generated ID
                string questionId = newQuestionDocRef.Id;

                // Save the document with the auto-generated ID
                await newQuestionDocRef.SetAsync(questionData);

                string imageUrl = string.Empty;

                // Check if an image file is provided in the request
                if (request.QuestionImage != null)
                {
                    // Folder structure: users/{Uid}/questionPapers/{QuestionPaperId}/questions/{QuestionId}/Image
                    string storageFolder = $"questions/{questionId}/Image";

                    // Upload the image to Firebase Storage
                    imageUrl = await FileManagementService.UploadImageToStorage(_storageClient, _bucketName, storageFolder, request.QuestionImage);

                    // Update the question document with the image location
                    var updateImageData = new Dictionary<string, object>
            {
                { "imageLocation", imageUrl }
            };
                    await newQuestionDocRef.UpdateAsync(updateImageData);
                }

                int totalLoggedTime = 0;
                var updateData = new Dictionary<string, object>
                {
                    { "totalLoggedTime", totalLoggedTime }
                };

                await newQuestionDocRef.UpdateAsync(updateData);

                // Prepare the result model
                var result = new QuestionResult
                {
                    Uid = request.Uid,
                    QuestionPaperId = request.QuestionPaperId,
                    QuestionId = questionId,
                    QuestionName = request.QuestionName,
                    QuestionDescription = request.QuestionDescription,
                    ImageLocation = imageUrl,
                    TotatLoggedTime = totalLoggedTime
                };

                return Ok(new { message = "Question created successfully", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }
    }
}
