using Amazon.SQS.Model;
using Amazon.SQS;
using Amazon.DynamoDBv2.DataModel;
using Newtonsoft.Json;
using Content.Models;

namespace Content.Services
{
    public class ImageProcessingNotificationService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IDynamoDBContext _dbContext;
        private readonly ILogger<ImageProcessingNotificationService> _logger;
        private readonly List<string> _queueUrls = new List<string>
        {
            "https://sqs.eu-north-1.amazonaws.com/474668427912/image-processed-notifications",
        };

        public ImageProcessingNotificationService(IAmazonSQS sqsClient, IDynamoDBContext dbContext, ILogger<ImageProcessingNotificationService> logger)
        {
            _sqsClient = sqsClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ImageProcessingNotificationService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var queueUrl in _queueUrls)
                {
                    await ProcessMessagesFromQueue(queueUrl, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("ImageProcessingNotificationService is stopping.");
        }

        private async Task ProcessMessagesFromQueue(string queueUrl, CancellationToken token)
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            };

            var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, token);
            foreach (var message in receiveMessageResponse.Messages)
            {
                _logger.LogInformation($"Processing message {message.Body}");
                try
                {
                    var notification = JsonConvert.DeserializeObject<SqsNotification>(message.Body);

                    var post = await _dbContext.LoadAsync<Post>(notification.PostId, token);

                    if (post != null)
                    {
                        post.ImageUrl = notification.ResizedImageUrl;
                        await _dbContext.SaveAsync(post, token);
                    }

                    var deleteMessageRequest = new DeleteMessageRequest
                    {
                        QueueUrl = queueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    };
                    await _sqsClient.DeleteMessageAsync(deleteMessageRequest, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing message from SQS queue {queueUrl}.");
                }
            }
        }
    }

    public class SqsNotification
    {
        public Guid PostId { get; set; }
        public string OriginalObjectKey { get; set; }
        public string ResizedObjectKey { get; set; }
        public string ResizedImageUrl { get; set; }
    }
}