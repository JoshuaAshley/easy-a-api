using easy_a_web_api.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace easy_a_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartController : ControllerBase
    {
        private readonly FirestoreDb _firestoreDb;

        public ChartController()
        {
            _firestoreDb = FireStoreService.DB!;
        }

        [HttpGet("logtime/{uid}")]
        public async Task<IActionResult> GetLoggedTimeForDateRange(string uid)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Retrieve all question papers for the user
                CollectionReference questionPapersCollection = userDocRef.Collection("questionPapers");
                QuerySnapshot questionPapersSnapshot = await questionPapersCollection.GetSnapshotAsync();

                // Dictionary to hold total logged time per question paper
                var questionPaperLoggedTime = new Dictionary<string, double>();

                // Loop through each question paper
                foreach (var questionPaperDoc in questionPapersSnapshot.Documents)
                {
                    string questionPaperName = questionPaperDoc.GetValue<string>("questionPaperName"); // Assuming this field holds the question paper name
                    double totalLoggedTime = 0;

                    // Reference to the 'questions' collection within each question paper
                    CollectionReference questionsCollection = questionPaperDoc.Reference.Collection("questions");
                    QuerySnapshot questionsSnapshot = await questionsCollection.GetSnapshotAsync();

                    // Process each question and sum logged time
                    foreach (var questionDoc in questionsSnapshot.Documents)
                    {
                        double loggedTime = questionDoc.GetValue<double>("totalLoggedTime"); // Assuming this field holds the logged time
                        totalLoggedTime += loggedTime;
                    }

                    // Store the total logged time for the question paper
                    questionPaperLoggedTime[questionPaperName] = totalLoggedTime;
                }

                // Prepare the result in a format suitable for a graph
                var result = questionPaperLoggedTime.Select(entry => new
                {
                    QuestionPaperName = entry.Key,
                    TotalLoggedTime = entry.Value
                }).ToList();

                return Ok(new { message = "Logged time retrieved successfully", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("completed/{uid}/{month}")]
        public async Task<IActionResult> GetCompletedStatusForMonth(string uid, string month)
        {
            try
            {
                // Get the number of days in the month
                int year = DateTime.UtcNow.Year; // Assuming the current year; you can modify this as needed
                DateTime firstDayOfMonth = new DateTime(year, DateTime.ParseExact(month, "MMMM", CultureInfo.InvariantCulture).Month, 1);
                int daysInMonth = DateTime.DaysInMonth(year, firstDayOfMonth.Month);

                // Dictionary to hold the completion status for each day
                var completionStatus = new Dictionary<string, bool>();

                // Initialize the dictionary with all days of the month set to false
                for (int day = 1; day <= daysInMonth; day++)
                {
                    completionStatus[firstDayOfMonth.AddDays(day - 1).ToString("yyyy-MM-dd")] = false;
                }

                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Retrieve all question papers for the user
                CollectionReference questionPapersCollection = userDocRef.Collection("questionPapers");
                QuerySnapshot questionPapersSnapshot = await questionPapersCollection.GetSnapshotAsync();

                // Loop through each question paper
                foreach (var questionPaperDoc in questionPapersSnapshot.Documents)
                {
                    // Reference to the 'questions' collection within each question paper
                    CollectionReference questionsCollection = questionPaperDoc.Reference.Collection("questions");
                    QuerySnapshot questionsSnapshot = await questionsCollection.GetSnapshotAsync();

                    // Process each question
                    foreach (var questionDoc in questionsSnapshot.Documents)
                    {
                        // Check for completedDate field
                        if (questionDoc.ContainsField("completedDate"))
                        {
                            DateTime completedDate = questionDoc.GetValue<DateTime>("completedDate");

                            // If the completed date falls within the specified month, update the status
                            if (completedDate.Year == year && completedDate.Month == firstDayOfMonth.Month)
                            {
                                completionStatus[completedDate.ToString("yyyy-MM-dd")] = true;
                            }
                        }
                    }
                }

                return Ok(new { message = "Completion status retrieved successfully", data = completionStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }
    }
}
