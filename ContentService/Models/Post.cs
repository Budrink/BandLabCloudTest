namespace Content.Models
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Caption { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // —сылка на .jpg файл
        public string OriginalImageUrl { get; set; } = string.Empty; // —сылка на оригинал
        public string Creator { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<Comment> Comments { get; set; }
    }

}
