namespace Content.Dto
{
    public class CommentDto
    {
        public Guid Id { get; set; } // Идентификатор комментария
        public string Content { get; set; } // Содержание комментария
        public string Creator { get; set; } // Имя создателя комментария
        public DateTime CreatedAt { get; set; } // Дата создания комментария
    }
}
