using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System;

namespace DemoCentral.Communication.Rabbit
{
    public interface IBlobStorage
    {
        Task DeleteBlobAsync(string blobUrl);
    }

    public class BlobStorage : IBlobStorage
    {
        private readonly BlobServiceClient _client;
        private readonly ILogger<BlobStorage> _logger;

        public BlobStorage(string connectionString, ILogger<BlobStorage> logger)
        {
            //This is here for further blob storage methods
            _client = new BlobServiceClient(connectionString);
            _logger = logger;
        }
        
        
        public async Task DeleteBlobAsync(string blobUrl)
        {
            _logger.LogInformation($"Attempting to delete blob at [ {blobUrl} ]");
            var client = new BlobClient(new Uri(blobUrl));
            var response = await client.DeleteIfExistsAsync();

            _logger.LogInformation($"Deleting blob at [ {blobUrl} ] successful");
        }
    }
}