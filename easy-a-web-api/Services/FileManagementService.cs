using Google.Cloud.Storage.V1;

namespace easy_a_web_api.Services
{
    public class FileManagementService
    {
        public static async Task<string> UploadPdfToStorage(StorageClient client, string bucketName, string storageFolder, IFormFile pdfFile)
        {
            // Define object name (path where the PDF will be stored)
            string fileName = $"{Guid.NewGuid().ToString()}.pdf";
            string filePath = $"{storageFolder}/{fileName}";

            // Upload PDF to Firebase Storage
            using (var memoryStream = new MemoryStream())
            {
                await pdfFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Upload object to Google Cloud Storage asynchronously
                await client.UploadObjectAsync(
                    bucketName,
                    filePath,
                    "application/pdf", // Specify content type here
                    memoryStream
                );
            }

            // Generate URL for the uploaded PDF
            string pdfUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{Uri.EscapeDataString(filePath)}?alt=media";

            return pdfUrl;
        }

        public static async Task<string> UploadImageToStorage(StorageClient client, string bucketName, string storageFolder, IFormFile imageFile)
        {
            // Define object name (path where the PDF will be stored)
            string fileName = $"{Guid.NewGuid().ToString()}.jpg";
            string filePath = $"{storageFolder}/{fileName}";

            // Upload PDF to Firebase Storage
            using (var memoryStream = new MemoryStream())
            {
                await imageFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Upload object to Google Cloud Storage asynchronously
                await client.UploadObjectAsync(
                    bucketName,
                    filePath,
                    "image/jpeg",
                    memoryStream
                );
            }

            // Generate URL for the uploaded PDF
            string imageUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucketName}/o/{Uri.EscapeDataString(filePath)}?alt=media";

            return imageUrl;
        }
    }
}
