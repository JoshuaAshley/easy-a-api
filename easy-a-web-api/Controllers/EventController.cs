using easy_a_web_api.Models.Event;
using easy_a_web_api.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace easy_a_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly FirestoreDb _firestoreDb;

        public EventController()
        {
            _firestoreDb = FireStoreService.DB!;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateEvent([FromForm] CreateEventRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Uid))
                {
                    return BadRequest(new { error = "Uid is required" });
                }

                if (string.IsNullOrEmpty(request.EventName) || request.EventDate == null)
                {
                    return BadRequest(new { error = "Event name and date are required" });
                }

                // Convert the event date to UTC (if not already in UTC)
                DateTime? eventDateUtc = request.EventDate.HasValue
                    ? DateTime.SpecifyKind(request.EventDate.Value, DateTimeKind.Unspecified).ToUniversalTime()
                    : null;

                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(request.Uid);

                // Reference to the 'events' collection inside the user's document
                CollectionReference eventsCollection = userDocRef.Collection("events");

                // Generate a new document ID for the event inside the 'events' collection
                DocumentReference newEventDocRef = eventsCollection.Document(); // Auto-generated ID
                string eventId = newEventDocRef.Id;

                // Prepare event data without the event date string
                var eventData = new
                {
                    eventName = request.EventName,
                    eventDate = eventDateUtc
                };

                // Save the event document
                await newEventDocRef.SetAsync(eventData);

                // Convert the event date to UTC+2 (South Africa Standard Time)
                string? eventDateUtcPlus2 = eventDateUtc.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(eventDateUtc.Value, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"))
                        .ToString("yyyy-MM-dd")
                    : null;

                // Prepare the result model with the converted event date (UTC+2)
                var result = new EventResult
                {
                    Uid = request.Uid,
                    EventId = eventId,
                    EventName = request.EventName,
                    EventDate = eventDateUtcPlus2
                };

                return Ok(new { message = "Event created successfully", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("list/{uid}")]
        public async Task<IActionResult> ListEvents(string uid)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the 'events' collection inside the user's document
                CollectionReference eventsCollection = userDocRef.Collection("events");

                // Retrieve all documents (events) in the collection
                QuerySnapshot eventsSnapshot = await eventsCollection.GetSnapshotAsync();

                var eventList = new List<EventResult>();

                foreach (DocumentSnapshot document in eventsSnapshot.Documents)
                {
                    DateTime eventDateUtc = document.GetValue<DateTime>("eventDate");

                    // Convert the event date from UTC to South Africa Standard Time (UTC+2)
                    string eventDateUtcPlus2 = TimeZoneInfo.ConvertTimeFromUtc(eventDateUtc, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"))
                        .ToString("yyyy-MM-dd");

                    var eventResult = new EventResult
                    {
                        Uid = uid,
                        EventId = document.Id,
                        EventName = document.GetValue<string>("eventName"),
                        EventDate = eventDateUtcPlus2 // Use the converted date string
                    };

                    eventList.Add(eventResult);
                }

                return Ok(new { message = "Events retrieved successfully", eventList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("{uid}/event/{eventId}")]
        public async Task<IActionResult> GetEvent(string uid, string eventId)
        {
            try
            {
                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the specific event document
                DocumentReference eventDocRef = userDocRef.Collection("events").Document(eventId);

                DocumentSnapshot eventSnapshot = await eventDocRef.GetSnapshotAsync();

                if (!eventSnapshot.Exists)
                {
                    return NotFound(new { error = "Event not found" });
                }

                DateTime eventDateUtc = eventSnapshot.GetValue<DateTime>("eventDate");

                // Convert the event date from UTC to South Africa Standard Time (UTC+2)
                string eventDateUtcPlus2 = TimeZoneInfo.ConvertTimeFromUtc(eventDateUtc, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"))
                    .ToString("yyyy-MM-dd");

                var eventResult = new EventResult
                {
                    Uid = uid,
                    EventId = eventSnapshot.Id,
                    EventName = eventSnapshot.GetValue<string>("eventName"),
                    EventDate = eventDateUtcPlus2 // Use the converted date string
                };

                return Ok(new { message = "Event retrieved successfully", eventResult });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet("list/{uid}/date/{eventDate}")]
        public async Task<IActionResult> GetEventsByDate(string uid, string eventDate)
        {
            try
            {
                // Parse the provided event date (in UTC+2)
                if (!DateTime.TryParse(eventDate, out DateTime parsedDate))
                {
                    return BadRequest(new { error = "Invalid date format. Please provide a valid date in yyyy-MM-dd format." });
                }

                // Convert the event date to the UTC time for querying Firestore
                DateTime startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(parsedDate.Date, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"));
                DateTime endOfDayUtc = startOfDayUtc.AddDays(1).AddTicks(-1);

                // Reference to the user's document
                DocumentReference userDocRef = _firestoreDb.Collection("users").Document(uid);

                // Reference to the 'events' collection inside the user's document
                CollectionReference eventsCollection = userDocRef.Collection("events");

                // Query the events collection for events occurring on the specified date
                Query query = eventsCollection.WhereGreaterThanOrEqualTo("eventDate", startOfDayUtc)
                                              .WhereLessThanOrEqualTo("eventDate", endOfDayUtc);

                QuerySnapshot eventsSnapshot = await query.GetSnapshotAsync();

                var eventList = new List<EventResult>();

                foreach (DocumentSnapshot document in eventsSnapshot.Documents)
                {
                    DateTime eventDateUtc = document.GetValue<DateTime>("eventDate");

                    // Convert the event date from UTC to South Africa Standard Time (UTC+2)
                    string eventDateUtcPlus2 = TimeZoneInfo.ConvertTimeFromUtc(eventDateUtc, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time"))
                        .ToString("yyyy-MM-dd");

                    var eventResult = new EventResult
                    {
                        Uid = uid,
                        EventId = document.Id,
                        EventName = document.GetValue<string>("eventName"),
                        EventDate = eventDateUtcPlus2 // Use the converted date string
                    };

                    eventList.Add(eventResult);
                }

                return Ok(new { message = "Events retrieved successfully", eventList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred: " + ex.Message });
            }
        }
    }
}
