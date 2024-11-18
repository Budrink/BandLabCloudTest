using Content.Models;
using Content.Dto;

namespace Content.Services
{
    public interface IPostService
    {
        Task<PostDto> CreatePostAsync(string caption, IFormFile image, string userName);
        Task<List<PostDto>> GetPostsAsync();
        Task<List<CommentDto>> GetPostCommentsAsync(Guid postId);
        Task<CommentDto> AddCommentAsync(Guid postId, string content, string userName);
        Task<bool> DeleteCommentAsync(Guid postId, Guid commentId, string userName);
    }

}