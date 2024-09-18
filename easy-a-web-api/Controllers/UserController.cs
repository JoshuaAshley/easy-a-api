using easy_a_web_api.Models.User;
using easy_a_web_api.Services;
using Firebase.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace easy_a_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly FirebaseAuthProvider _authProvider;

        public UserController()
        {
            string firebaseAPIKey = Environment.GetEnvironmentVariable("FIREBASE_API_KEY")!;

            // Initialize Firebase Auth
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(firebaseAPIKey));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Register user with Firebase Auth
                var result = await _authProvider.CreateUserWithEmailAndPasswordAsync(request.Email, request.Password);

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
                    Email = request.Email,
                    FirstName = request.FirstName ?? string.Empty,
                    LastName = request.LastName ?? string.Empty,
                    Gender = request.Gender ?? string.Empty,
                    DateOfBirth = request.DateOfBirth.ToString() ?? null,
                    ProfilePicture = request.ProfilePicture ?? string.Empty
                };

                return Ok(registerResult);
            }
            catch (FirebaseAuthException ex)
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
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
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
                    FirstName = userData.ContainsKey("firstname") ? userData["firstname"].ToString() : null,
                    LastName = userData.ContainsKey("lastname") ? userData["lastname"].ToString() : null,
                    Gender = userData.ContainsKey("gender") ? userData["gender"].ToString() : null,
                    DateOfBirth = userData.ContainsKey("dob") ? userData["dob"].ToString() : null,
                    ProfilePicture = userData.ContainsKey("pfp") ? userData["pfp"].ToString() : null
                };

                return Ok(loginResult);
            }
            catch (FirebaseAuthException ex)
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
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
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

                // Prepare updated user data
                var userData = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(request.FirstName))
                    userData["firstname"] = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    userData["lastname"] = request.LastName;

                if (!string.IsNullOrEmpty(request.Gender))
                    userData["gender"] = request.Gender;

                if (request.DateOfBirth.HasValue)
                    userData["dob"] = request.DateOfBirth.Value;

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

    }
}