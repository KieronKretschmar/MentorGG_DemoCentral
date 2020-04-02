using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System;
using System.Web;
using System.Linq;

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
            var urlQuerySections = blobUrl.Split("/");

            var blobName = urlQuerySections.Last();
            var blobContainerName = urlQuerySections[urlQuerySections.Length - 2];

            var blobClient = _client.GetBlobContainerClient(blobContainerName).GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync();

            _logger.LogInformation($"Deleting blob at [ {blobUrl} ] successful");
        }
    }
}