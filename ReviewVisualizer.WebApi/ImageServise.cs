using Azure.Storage.Blobs;
using System.Drawing;
using System.Drawing.Imaging;

namespace VisualizerProject
{
    public class ImageService
    {
        private static readonly string[] permittedPhotoExtensions = { ".png", ".jpeg" };
        private readonly WebApplicationBuilder _builder;

        public ImageService(WebApplicationBuilder builder)
        {
            _builder = builder;
        }

        public async Task<string> UploadImageAsync(IFormFile image)
        {
            var imageName = $"departments_{Guid.NewGuid()}{Path.GetExtension(image.FileName).ToLowerInvariant()}";

            if (_builder.Environment.IsProduction())
            {
                // If Production, save image in Blob Storage.
                var blobClient = await GetBlobClientForImageAsync(imageName);

                using (var stream = image.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                return imageName;
            }

            // If not Production, save image on local computer
            using var memoryStream = new MemoryStream();
            image.CopyTo(memoryStream);

            Image imageFile = Image.FromStream(memoryStream);
            ImageFormat imageFormat = GetImageFormat(image);
            imageFile.Save(Path.Combine(_builder.Configuration["ImagesStorage"]!, imageName), imageFormat);

            return imageName;
        }

        public bool ValidateImage(IFormFile image)
        {
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && permittedPhotoExtensions.Contains(ext);
        }

        public async Task DeleteImageAsync(string imgName)
        {
            if (_builder.Environment.IsProduction())
            {
                var blobClient = await GetBlobClientForImageAsync(imgName);

                var deleted = await blobClient.DeleteIfExistsAsync();

                if (!deleted.Value)
                    throw new InvalidOperationException($"Cannot delete image {imgName}");

                return;
            }

            string imageStorageFolder = _builder.Configuration["ImagesStorage"]!;
            string imgFullPath = Path.Combine(imageStorageFolder, imgName);

            if (File.Exists(imgFullPath))
                File.Delete(imgFullPath);

            return;
        }

        private ImageFormat GetImageFormat(IFormFile image)
        {
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            return ext switch
            {
                "png" => ImageFormat.Png,
                "jpeg" => ImageFormat.Jpeg,
                _ => throw new ArgumentException(nameof(image.FileName))
            };
        }

        private async Task<BlobClient> GetBlobClientForImageAsync(string imageName)
        {
            var blobServiceClient =
                    new BlobServiceClient(_builder.Configuration["ImagesStorage:Url"]);

            var containerClient = blobServiceClient.GetBlobContainerClient(_builder.Configuration["ImagesStorage:ContainerName"]);

            var response = await containerClient.CreateIfNotExistsAsync();
            if (response != null)
            {
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            }

            var blobClient = containerClient.GetBlobClient(imageName);

            return blobClient;
        }
    }
}