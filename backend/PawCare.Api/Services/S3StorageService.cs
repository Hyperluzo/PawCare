using System.Text.RegularExpressions;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace PawCare.Api.Services
{
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3? _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<S3StorageService> _logger;

        private readonly string _bucketName;
        private readonly string _region;
        private readonly bool _simulationMode;
        private readonly HashSet<string> _allowedExtensions;
        private readonly long _maxFileSizeBytes;

        public bool IsSimulationMode => _simulationMode;
        public string BucketName => _bucketName;

        public S3StorageService(
            IConfiguration configuration,
            ILogger<S3StorageService> logger,
            IAmazonS3? s3Client = null)
        {
            _configuration = configuration;
            _logger = logger;
            _bucketName = configuration["Aws:S3:BucketName"] ?? "pawcare-attachments-production";
            _region = configuration["Aws:Region"] ?? "us-east-1";
            _simulationMode = configuration.GetValue<bool>("Aws:S3:SimulationMode", false);

            var extensions = configuration.GetSection("Aws:S3:AllowedExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".doc", ".docx" };
            _allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

            _maxFileSizeBytes = configuration.GetValue<long>("Aws:S3:MaxFileSizeBytes", 10 * 1024 * 1024); // 10MB default

            if (s3Client != null)
            {
                _s3Client = s3Client;
            }
            else if (!_simulationMode)
            {
                try
                {
                    var regionEndpoint = RegionEndpoint.GetBySystemName(_region);
                    _s3Client = new AmazonS3Client(regionEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize AmazonS3Client. Falling back to S3 simulation mode.");
                    _simulationMode = true;
                }
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "attachments")
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                throw new ArgumentException("File stream cannot be empty.", nameof(fileStream));
            }

            if (fileStream.Length > _maxFileSizeBytes)
            {
                throw new InvalidOperationException($"File exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)}MB.");
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension) || !_allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"File extension '{extension}' is not permitted. Allowed: {string.Join(", ", _allowedExtensions)}");
            }

            // Sanitize file name
            var cleanFileName = Regex.Replace(Path.GetFileNameWithoutExtension(fileName), @"[^a-zA-Z0-9_\-]", "_");
            if (cleanFileName.Length > 50) cleanFileName = cleanFileName[..50];
            var key = $"{folder.Trim('/')}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{cleanFileName}{extension.ToLowerInvariant()}";

            if (_simulationMode || _s3Client == null)
            {
                _logger.LogInformation("[AWS S3 Simulation] Uploaded {FileName} ({SizeBytes} bytes) to s3://{Bucket}/{Key}",
                    fileName, fileStream.Length, _bucketName, key);
                return GetPublicUrl(key);
            }

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
                };

                await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation("Successfully uploaded {FileName} to AWS S3 bucket {Bucket} with key {Key}",
                    fileName, _bucketName, key);

                return GetPublicUrl(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload {FileName} to AWS S3 bucket {Bucket}", fileName, _bucketName);
                throw new ApplicationException($"Failed to upload file to S3: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrlOrKey)
        {
            if (string.IsNullOrWhiteSpace(fileUrlOrKey)) return false;

            var key = ExtractKey(fileUrlOrKey);
            if (string.IsNullOrWhiteSpace(key)) return false;

            if (_simulationMode || _s3Client == null)
            {
                _logger.LogInformation("[AWS S3 Simulation] Deleted object {Key} from {Bucket}", key, _bucketName);
                return true;
            }

            try
            {
                await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                });
                _logger.LogInformation("Deleted object {Key} from AWS S3 bucket {Bucket}", key, _bucketName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting object {Key} from AWS S3 bucket {Bucket}", key, _bucketName);
                return false;
            }
        }

        public string GetPublicUrl(string key)
        {
            return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key.TrimStart('/')}";
        }

        private string ExtractKey(string fileUrlOrKey)
        {
            if (!fileUrlOrKey.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !fileUrlOrKey.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return fileUrlOrKey;
            }

            try
            {
                var uri = new Uri(fileUrlOrKey);
                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return fileUrlOrKey;
            }
        }
    }
}
