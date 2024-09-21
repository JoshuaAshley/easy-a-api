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
                    string storageFolder = $"questions/{questionId}";

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
                bool isCompleted = false;

                var updateData = new Dictionary<string, object>
                {
                    { "totalLoggedTime", totalLoggedTime },
                    { "isCompleted", isCompleted }
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
                    TotatLoggedTime = totalLoggedTime,
                    IsCompleted = isCompleted,
                };

                DocumentSnapshot questionPaperSnapshot = await questionPaperDocRef.GetSnapshotAsync();

                int numQuestions = questionPaperSnapshot.ContainsField("numQuestions") ? questionPaperSnapshot.GetValue<int>("numQuestions") : 0;

                var updateQPData = new Dictionary<string, object>
                {
                    { "numQuestions", numQuestions + 1 }
                };

                await questionPaperDocRef.UpdateAsync(updateQPData);

                return Ok(new { message = "Question created successfully", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("{uid}/question-paper/{questionPaperId}/questions")]
        public async Task<IActionResult> GetQuestionsByQuestionPaperId(string uid, string questionPaperId)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the specific question paper document
                DocumentReference questionPaperDocRef = userDocRef.Collection("questionPapers").Document(questionPaperId);

                // Reference to the 'questions' collection inside the question paper
                CollectionReference questionsCollection = questionPaperDocRef.Collection("questions");

                // Get all documents in the 'questions' collection
                QuerySnapshot questionsSnapshot = await questionsCollection.GetSnapshotAsync();

                var questionsList = questionsSnapshot.Documents.Select(doc => new QuestionResult
                {
                    Uid = uid,
                    QuestionPaperId = questionPaperId,
                    QuestionId = doc.Id,
                    QuestionName = doc.ContainsField("questionName") ? doc.GetValue<string>("questionName") : "",
                    QuestionDescription = doc.ContainsField("questionDescription") ? doc.GetValue<string>("questionDescription") : "",
                    ImageLocation = doc.ContainsField("imageLocation") ? doc.GetValue<string>("imageLocation") : "",
                    TotatLoggedTime = doc.ContainsField("totalLoggedTime") ? doc.GetValue<int>("totalLoggedTime") : 0,
                    IsCompleted = doc.ContainsField("isCompleted") ? doc.GetValue<bool>("isCompleted") : false,
                }).ToList();

                return Ok(new { questions = questionsList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("{uid}/question-paper/{questionPaperId}/questions/{questionId}")]
        public async Task<IActionResult> GetQuestionById(string uid, string questionPaperId, string questionId)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the specific question paper document
                DocumentReference questionPaperDocRef = userDocRef.Collection("questionPapers").Document(questionPaperId);

                // Reference to the specific question document
                DocumentReference questionDocRef = questionPaperDocRef.Collection("questions").Document(questionId);

                // Get the document snapshot
                DocumentSnapshot questionSnapshot = await questionDocRef.GetSnapshotAsync();

                if (!questionSnapshot.Exists)
                {
                    return NotFound(new { error = "Question not found" });
                }

                // Prepare the result
                var questionResult = new QuestionResult
                {
                    Uid = uid,
                    QuestionPaperId = questionPaperId,
                    QuestionId = questionSnapshot.Id,
                    QuestionName = questionSnapshot.ContainsField("questionName") ? questionSnapshot.GetValue<string>("questionName") : "",
                    QuestionDescription = questionSnapshot.ContainsField("questionDescription") ? questionSnapshot.GetValue<string>("questionDescription") : "",
                    ImageLocation = questionSnapshot.ContainsField("imageLocation") ? questionSnapshot.GetValue<string>("imageLocation") : "",
                    TotatLoggedTime = questionSnapshot.ContainsField("totalLoggedTime") ? questionSnapshot.GetValue<int>("totalLoggedTime") : 0,
                    IsCompleted = questionSnapshot.ContainsField("isCompleted") ? questionSnapshot.GetValue<bool>("isCompleted") : false,
                };

                return Ok(questionResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost("{uid}/question-paper/{questionPaperId}/questions/{questionId}/log-time")]
        public async Task<IActionResult> LogTime(string uid, string questionPaperId, string questionId, [FromForm] int TimeLogged)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the specific question paper document
                DocumentReference questionPaperDocRef = userDocRef.Collection("questionPapers").Document(questionPaperId);

                // Reference to the specific question document
                DocumentReference questionDocRef = questionPaperDocRef.Collection("questions").Document(questionId);

                // Check if the question exists
                DocumentSnapshot questionSnapshot = await questionDocRef.GetSnapshotAsync();

                if (!questionSnapshot.Exists)
                {
                    return NotFound(new { error = "Question not found" });
                }

                // Get the current total logged time
                double currentTotalLoggedTime = questionSnapshot.ContainsField("totalLoggedTime")
                    ? questionSnapshot.GetValue<double>("totalLoggedTime")
                    : 0;

                // Update the total logged time
                double updatedTotalLoggedTime = currentTotalLoggedTime + TimeLogged;

                // Update the question document with the new total logged time
                await questionDocRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "totalLoggedTime", updatedTotalLoggedTime }
                });

                return Ok(new { message = "Logged time updated successfully", totalLoggedTime = updatedTotalLoggedTime });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost("{uid}/question-paper/{questionPaperId}/questions/{questionId}/complete")]
        public async Task<IActionResult> CompleteQuestion(string uid, string questionPaperId, string questionId)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the specific question paper document
                DocumentReference questionPaperDocRef = userDocRef.Collection("questionPapers").Document(questionPaperId);

                // Reference to the specific question document
                DocumentReference questionDocRef = questionPaperDocRef.Collection("questions").Document(questionId);

                // Check if the question exists and is not already completed
                DocumentSnapshot questionSnapshot = await questionDocRef.GetSnapshotAsync();

                if (!questionSnapshot.Exists)
                {
                    return NotFound(new { error = "Question not found" });
                }

                bool isAlreadyCompleted = questionSnapshot.ContainsField("isCompleted") ? questionSnapshot.GetValue<bool>("isCompleted") : false;

                if (isAlreadyCompleted)
                {
                    return BadRequest(new { error = "Question is already marked as completed." });
                }

                // Get the current date and time in UTC
                DateTime completedDate = DateTime.UtcNow;

                // Update the isCompleted field and completedDate field of the question
                await questionDocRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "isCompleted", true },
                    { "completedDate", completedDate }
                });

                // Increment the numCompletedQuestions field of the question paper
                DocumentSnapshot questionPaperSnapshot = await questionPaperDocRef.GetSnapshotAsync();
                int numCompletedQuestions = questionPaperSnapshot.ContainsField("numCompletedQuestions")
                    ? questionPaperSnapshot.GetValue<int>("numCompletedQuestions")
                    : 0;

                var updateQPData = new Dictionary<string, object>
                {
                    { "numCompletedQuestions", numCompletedQuestions + 1 }
                };

                await questionPaperDocRef.UpdateAsync(updateQPData);

                return Ok(new { message = "Question marked as completed, completedDate set, and numCompletedQuestions incremented" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }
    }
}
