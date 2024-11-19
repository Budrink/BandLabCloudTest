using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageProcessor
{
    public class Function
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonSQS _sqsClient;
        // todo: replace by loading from env variables
        private readonly string _notificationQueueUrl = "https://sqs.eu-north-1.amazonaws.com/474668427912/image-processed-notifications";

        public Function()
        {
            _s3Client = new AmazonS3Client();
            _sqsClient = new AmazonSQSClient();
        }

        public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            foreach (var record in sqsEvent.Records)
            {
                context.Logger.LogLine($"Processing message {record.Body}");
                var messageBody = JsonConvert.DeserializeObject<S3EventNotification>(record.Body);
                context.Logger.LogLine($"Processing message {messageBody}");
                var bucketName = messageBody.BucketName;
                var postId = messageBody.PostId;
                var objectKey = messageBody.ObjectKey;
                var resizedObjectKey = $"resized-{objectKey}";

                try
                {
                    var response = await _s3Client.GetObjectAsync(bucketName, objectKey);
                    using (var responseStream = response.ResponseStream)
                    using (var image = Image.Load(responseStream))
                    {
                        image.Mutate(x => x.Resize(600, 600));
                        using (var memoryStream = new MemoryStream())
                        {
                            image.SaveAsJpeg(memoryStream);
                            memoryStream.Position = 0;
                            await _s3Client.PutObjectAsync(new PutObjectRequest
                            {
                                BucketName = bucketName,
                                Key = resizedObjectKey,
                                InputStream = memoryStream
                            });
                        }
                    }

                    // Генерация предподписанного URL для обработанного изображения
                    var url = GeneratePreSignedURL(bucketName, resizedObjectKey);
                    context.Logger.LogLine($"Generated URL: {url}");

                    // Отправка уведомления в SQS
                    var notification = new
                    {
                        PostId = postId,
                        OriginalObjectKey = objectKey,
                        ResizedObjectKey = resizedObjectKey,
                    };

                    var sendMessageRequest = new SendMessageRequest
                    {
                        QueueUrl = _notificationQueueUrl,
                        MessageBody = JsonConvert.SerializeObject(notification)
                    };

                    await _sqsClient.SendMessageAsync(sendMessageRequest);
                }
                catch (AmazonS3Exception e)
                {
                    context.Logger.LogLine($"Error getting object {objectKey} from bucket {bucketName}. Make sure they exist and your bucket is in the same region as this function.");
                    context.Logger.LogLine(e.Message);
                }
                catch (Exception e)
                {
                    context.Logger.LogLine($"Unknown error: {e.Message}");
                }
            }
        }

        private string GeneratePreSignedURL(string bucketName, string objectKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddHours(1) // URL будет действителен в течение 1 часа
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }

    public class S3EventNotification
    {
        public string PostId { get; set; }
        public string BucketName { get; set; }
        public string ObjectKey { get; set; }
    }
}
