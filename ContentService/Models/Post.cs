using Amazon.DynamoDBv2.DataModel;

namespace Content.Models
{
    [DynamoDBTable("Post")]
    public class Post
    {
        [DynamoDBHashKey]
        public Guid Id { get; set; }
        public string Caption { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // —сылка на .jpg файл
        public string OriginalImageUrl { get; set; } = string.Empty; // —сылка на оригинал
        public string Creator { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<Comment> Comments { get; set; }
    }

}
