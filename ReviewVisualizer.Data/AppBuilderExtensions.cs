using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ReviewVisualizer.Data
{
    public static class AppBuilderExtensions
    {
        public static void AddDataProtection(this WebApplicationBuilder builder)
        {
            if (builder.Environment.IsProduction())
            {
                var blobServiceClient =
                    new BlobServiceClient(builder.Configuration["DataProtection:Url"]);

                // Create/get container "dataprotection"
                var containerClient = blobServiceClient.GetBlobContainerClient(builder.Configuration["DataProtection:ContainerName"]);
                containerClient.CreateIfNotExists();

                // Persist Data Protection keys into blob
                builder.Services
                    .AddDataProtection()
                    .PersistKeysToAzureBlobStorage(containerClient.GetBlobClient(builder.Configuration["DataProtection:KeyName"]!))
                    .SetApplicationName(builder.Configuration["AppName"]!);

                return;
            }

            builder.Services.AddDataProtection()
                   .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:FilePath"]!))
                   .SetApplicationName(builder.Configuration["AppName"]!);
        }
    }
}
