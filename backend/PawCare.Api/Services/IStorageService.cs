namespace PawCare.Api.Services
{
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a file stream to S3 (or simulated S3 storage) and returns the full access URL.
        /// </summary>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "attachments");

        /// <summary>
        /// Deletes a file from S3 by URL or object key.
        /// </summary>
        Task<bool> DeleteFileAsync(string fileUrlOrKey);

        /// <summary>
        /// Returns the public or S3 URL for a given object key.
        /// </summary>
        string GetPublicUrl(string key);

        /// <summary>
        /// True if running in offline simulation mode without contacting real AWS S3 endpoints.
        /// </summary>
        bool IsSimulationMode { get; }

        /// <summary>
        /// S3 Bucket name currently configured.
        /// </summary>
        string BucketName { get; }
    }
}
