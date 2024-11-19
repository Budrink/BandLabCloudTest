using Amazon.DynamoDBv2.DataModel;

namespace Content.Models
{
    [DynamoDBTable("Comment")]
    public class Comment
    {
        [DynamoDBHashKey]
        public Guid PostId { get; set; }

        [DynamoDBRangeKey]
        public Guid Id { get; set; }

        public string Content { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}