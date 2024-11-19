﻿using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Content.Dto;
using Content.Models;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;

namespace Content.Services
{
    public class PostService : IPostService
    {
        private readonly IDynamoDBContext _dbContext;
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;


        public PostService(IDynamoDBContext dbContext, IAmazonSQS sqsClient, IConfiguration configuration, IAmazonS3 s3Client)
        {
            _dbContext = dbContext;
            _sqsClient = sqsClient;
            _queueUrl = "https://sqs.eu-north-1.amazonaws.com/474668427912/image-loaded-notification";
            _bucketName = configuration["S3BucketName"]; // Извлечение имени бакета из конфигурации
            _s3Client = s3Client;
        }

        public async Task<PostDto> CreatePostAsync(string caption, IFormFile image, string userName)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("No image uploaded.", nameof(image));

            var originalFileName = $"{Path.GetFileNameWithoutExtension(image.FileName)}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var fileTransferUtility = new Amazon.S3.Transfer.TransferUtility(_s3Client);

            using (var newMemoryStream = new MemoryStream())
            {
                await image.CopyToAsync(newMemoryStream);
                await fileTransferUtility.UploadAsync(newMemoryStream, _bucketName, originalFileName);
            }

            var imageUrl = $"https://{_bucketName}.s3.amazonaws.com/{originalFileName}";


            var post = new Post
            {
                Id = Guid.NewGuid(),
                Caption = caption,
                ImageUrl = imageUrl,
                Creator = userName ?? "Anonymous",
                CreatedAt = DateTime.UtcNow,
                Comments = new List<Comment>()
            };

            await _dbContext.SaveAsync(post);

            var message = new
            {
                BucketName = _bucketName,
                ObjectKey = originalFileName,
                PostId = post.Id
            };

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(message)
            };

            var response = await _sqsClient.SendMessageAsync(sendMessageRequest);

            return new PostDto
            {
                Id = post.Id,
                Caption = post.Caption,
                ImageUrl = post.ImageUrl,
                CreatedAt = post.CreatedAt,
                LastTwoComments = new List<CommentDto>()
            };
        }

        public async Task<List<PostDto>> GetPostsAsync()
        {
            var conditions = new List<ScanCondition>();
            var posts = await _dbContext.ScanAsync<Post>(conditions).GetRemainingAsync();

            return posts
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Caption = p.Caption,
                    ImageUrl = p.ImageUrl,
                    CreatedAt = p.CreatedAt,
                    LastTwoComments = p.Comments
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(2)
                        .Select(c => new CommentDto
                        {
                            Id = c.Id,
                            Content = c.Content,
                            Creator = c.Creator,
                            CreatedAt = c.CreatedAt
                        })
                        .ToList()
                })
                .ToList();
        }

        public async Task<List<CommentDto>> GetPostCommentsAsync(Guid postId)
        {
            var post = await _dbContext.LoadAsync<Post>(postId);

            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            return post.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    Creator = c.Creator,
                    CreatedAt = c.CreatedAt
                })
                .ToList();
        }

        public async Task<CommentDto> AddCommentAsync(Guid postId, string content, string creator)
        {
            var post = await _dbContext.LoadAsync<Post>(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = content,
                Creator = creator,
                CreatedAt = DateTime.UtcNow,
                PostId = postId
            };

            post.Comments.Add(comment);
            await _dbContext.SaveAsync(post);

            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                Creator = comment.Creator,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<bool> DeleteCommentAsync(Guid postId, Guid commentId, string currentUser)
        {
            var post = await _dbContext.LoadAsync<Post>(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            var comment = post.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null)
                throw new KeyNotFoundException("Comment not found.");

            if (comment.Creator != currentUser)
                throw new UnauthorizedAccessException("You are not authorized to delete this comment.");

            post.Comments.Remove(comment);
            await _dbContext.SaveAsync(post);
            return true;
        }
    }
}
