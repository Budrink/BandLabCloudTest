
namespace Content.Dto
{
    public class PostDto
    {
        public Guid Id { get; set; } // Идентификатор поста
        public string Caption { get; set; } // Подпись к изображению
        public string ImageUrl { get; set; } // URL изображения
        public DateTime CreatedAt { get; set; } // Дата создания поста
        public List<CommentDto> LastTwoComments { get; set; } // Последние два комментария
    }
}
