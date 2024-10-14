using easy_a_web_api.Models.User;
using easy_a_web_api.Services;
using Firebase.Auth;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;

namespace easy_a_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly FirebaseAuthProvider _authProvider;
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "easy-a-dbad0.appspot.com";

        public UserController()
        {
            string firebaseAPIKey = Environment.GetEnvironmentVariable("FIREBASE_API_KEY")!;

            // Initialize Firebase Auth
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(firebaseAPIKey));

            _storageClient = FireStoreService.StorageClient!;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Register user with Firebase Auth
                var result = await _authProvider.CreateUserWithEmailAndPasswordAsync(request.Email, request.Password);

                var token = result.FirebaseToken;

                // Get the UID of the created user
                var userId = result.User.LocalId;

                // Prepare Firestore user data
                var userData = new
                {
                    uid = userId,
                    firstname = request.FirstName ?? string.Empty,
                    lastname = request.LastName ?? string.Empty,
                    gender = request.Gender ?? string.Empty,
                    dob = request.DateOfBirth ?? null,
                    pfp = request.ProfilePicture ?? string.Empty
                };

                // Add user document to Firestore
                DocumentReference docRef = FireStoreService.DB!.Collection("users").Document(userId);
                await docRef.SetAsync(userData);

                // Create the response result with registered user details
                var registerResult = new UserResult
                {
                    Uid = userId,
                    Token = token,
                    Email = request.Email,
                    FirstName = request.FirstName ?? string.Empty,
                    LastName = request.LastName ?? string.Empty,
                    Gender = request.Gender ?? string.Empty,
                    DateOfBirth = request.DateOfBirth.ToString() ?? null,
                    ProfilePicture = request.ProfilePicture ?? string.Empty
                };

                return Ok(registerResult);
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                if (ex.Reason == AuthErrorReason.EmailExists)
                {
                    return BadRequest(new { error = "Email already registered" });
                }

                return StatusCode(500, new { error = "Registration failed: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        /// <summary>
        /// Logs in a user with email and password.
        /// </summary>
        /// <param name="request">LoginRequest object containing email and password.</param>
        /// <returns>A response with an authentication token and user details if successful, or an error message if authentication fails.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Authenticate user with Firebase Auth
                var authResult = await _authProvider.SignInWithEmailAndPasswordAsync(request.Email, request.Password);

                // Extract token and user details
                var token = authResult.FirebaseToken;
                var userId = authResult.User.LocalId;

                // Retrieve user details from Firestore
                DocumentReference docRef = FireStoreService.DB!.Collection("users").Document(userId);
                var userDoc = await docRef.GetSnapshotAsync();

                if (!userDoc.Exists)
                {
                    return NotFound(new { error = "User details not found" });
                }

                // Extract user data from Firestore document
                var userData = userDoc.ToDictionary();

                // Prepare response
                var loginResult = new UserResult
                {
                    Uid = userId,
                    Token = token,
                    Email = request.Email,
                    FirstName = userData.ContainsKey("firstname") ? userData["firstname"]?.ToString() ?? string.Empty : string.Empty,
                    LastName = userData.ContainsKey("lastname") ? userData["lastname"]?.ToString() ?? string.Empty : string.Empty,
                    Gender = userData.ContainsKey("gender") ? userData["gender"]?.ToString() ?? string.Empty : string.Empty,
                    DateOfBirth = userData.ContainsKey("dob") ? userData["dob"]?.ToString() ?? string.Empty : string.Empty,
                    ProfilePicture = userData.ContainsKey("pfp") ? userData["pfp"]?.ToString() ?? string.Empty : string.Empty
                };

                return Ok(loginResult);
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                if (ex.Reason == AuthErrorReason.InvalidEmailAddress || ex.Reason == AuthErrorReason.WrongPassword)
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                return StatusCode(500, new { error = "Login failed: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        /// <summary>
        /// Updates the details of a user in Firestore.
        /// </summary>
        /// <param name="request">An UpdateUserRequest object containing the UID and updated details of the user.</param>
        /// <returns>A response indicating whether the update was successful or if there was an error.</returns>
        /// <remarks>
        /// Requires the UID of the user to ensure the correct user is being updated. Only fields provided in the request will be updated.
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Retrieve user document from Firestore
                DocumentReference docRef = FireStoreService.DB!.Collection("users").Document(request.Uid);
                var userDoc = await docRef.GetSnapshotAsync();

                if (!userDoc.Exists)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Convert the question paper due date to UTC (if not already in UTC)
                DateTime? dob = request.DateOfBirth?.ToUniversalTime();

                // Prepare updated user data
                var userData = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(request.FirstName))
                    userData["firstname"] = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    userData["lastname"] = request.LastName;

                if (!string.IsNullOrEmpty(request.Gender))
                    userData["gender"] = request.Gender;

                if (dob.HasValue)
                    userData["dob"] = dob;

                string imageUrl = string.Empty;

                // Check if a PDF file is provided in the request
                if (request.ProfileImage != null)
                {
                    // Folder structure: users/{Uid}/questionPapers/{QuestionPaperId}/PDF
                    string storageFolder = $"profile-photos/{request.Uid}";

                    // Upload the PDF to Firebase Storage
                    imageUrl = await FileManagementService.UploadImageToStorage(_storageClient, _bucketName, storageFolder, request.ProfileImage);

                    // Update the question paper document with the PDF location
                    userData["pfp"] = imageUrl;
                }

                // Update the user document in Firestore
                await docRef.UpdateAsync(userData);

                // Retrieve updated user details from Firestore
                var updatedUserDoc = await docRef.GetSnapshotAsync();
                var updatedUserData = updatedUserDoc.ToDictionary();

                // Prepare response with updated user details
                var userResult = new UserResult
                {
                    Uid = request.Uid,
                    FirstName = updatedUserData.ContainsKey("firstname") ? updatedUserData["firstname"].ToString() : null,
                    LastName = updatedUserData.ContainsKey("lastname") ? updatedUserData["lastname"].ToString() : null,
                    Gender = updatedUserData.ContainsKey("gender") ? updatedUserData["gender"].ToString() : null,
                    DateOfBirth = updatedUserData.ContainsKey("dob") ? updatedUserData["dob"].ToString() : null,
                    ProfilePicture = updatedUserData.ContainsKey("pfp") ? updatedUserData["pfp"].ToString() : null
                };

                return Ok(userResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        [HttpPost("google-signin")]
        public async Task<IActionResult> GoogleSignIn([FromForm] GoogleSignInRequest request)
        {
            try
            {

                // Check if the user exists in Firestore using the email instead of the token
                DocumentReference docRef = FireStoreService.DB!.Collection("users").Document(request.Uid); // Use decodedToken.Uid for document ID
                var userDoc = await docRef.GetSnapshotAsync();

                // If the user does not exist, create a new user in Firestore
                if (!userDoc.Exists)
                {
                    var userData = new
                    {
                        uid = request.Uid, // Use uid from the decoded token
                        firstname = request.Firstname ?? "",
                        lastname = request.LastName ?? "",
                        email = request.Email ?? "", // Use email from the request
                        pfp = request.ProfilePicture ?? "" // Profile picture can be null
                    };

                    await docRef.SetAsync(userData);
                }

                // Return user data with email
                var userResult = new UserResult
                {
                    Uid = request.Uid, // Use uid from the decoded token
                    Email = request.Email ?? "", // Use email from the request
                    FirstName = request.LastName ?? "",
                    LastName = request.LastName ?? "",
                    ProfilePicture = request.ProfilePicture ?? ""
                };

                return Ok(userResult);
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"FirebaseAuthException: {ex.Message}");
                return Unauthorized(new { error = "Invalid Google token: " + ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                Console.WriteLine($"General Exception: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        [HttpPost("check-user-exists")]
        public async Task<IActionResult> CheckUserExists([FromForm] CheckUserExistsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Query Firestore to check if a user with the specified email exists
                var query = FireStoreService.DB!.Collection("users")
                    .WhereEqualTo("email", request.Email);

                var userSnapshot = await query.GetSnapshotAsync();

                // Determine if any user document exists with the provided email
                bool userExists = userSnapshot.Documents.Count > 0;

                return Ok(new { exists = userExists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount([FromForm] DeleteAccountRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Authenticate user with Firebase Auth
                var authResult = await _authProvider.SignInWithEmailAndPasswordAsync(request.Email, request.Password);
                var userId = authResult.User.LocalId;

                // Delete user document from Firestore
                DocumentReference docRef = FireStoreService.DB!.Collection("users").Document(userId);
                await docRef.DeleteAsync();

                // Delete user folders from Firebase Storage
                await DeleteUserFolders(_storageClient, _bucketName, userId);

                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                if (ex.Reason == AuthErrorReason.InvalidEmailAddress || ex.Reason == AuthErrorReason.WrongPassword)
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                return StatusCode(500, new { error = "Account deletion failed: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error: " + ex.Message });
            }
        }

        public static async Task DeleteUserFolders(StorageClient storageClient, string bucketName, string userId)
        {
            var foldersToDelete = new List<string>
            {
                $"profile-photos/{userId}",
                $"question-papers/{userId}",
                $"questions/{userId}"
            };

            foreach (var folder in foldersToDelete)
            {
                var files = storageClient.ListObjects(bucketName, folder);
                foreach (var file in files)
                {
                    await storageClient.DeleteObjectAsync(bucketName, file.Name);
                }
            }
        }
    }
}