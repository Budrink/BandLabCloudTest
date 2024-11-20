using Content.Data;
using Content.Models;
using Content.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Content.Endpoints
{
    public static class PostEndpoints
    {
        public static void MapPostEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/healthcheck", () => Results.Ok("Healthy!"));

            routes.MapPost("/api/posts/create",
                [Authorize] async (IPostService postService, string caption, IFormFile image, HttpContext httpContext) =>
                {
                    var userName = httpContext.User.Identity?.Name;
                    var post = await postService.CreatePostAsync(caption, image, userName);
                    return Results.Ok(post);
                }).DisableAntiforgery();

            routes.MapGet("/api/posts", async (IPostService postService) =>
            {
                var posts = await postService.GetPostsAsync();
                return Results.Ok(posts);
            });

            routes.MapGet("/api/posts/{postId}/comments", async (Guid postId, IPostService postService) =>
            {
                try
                {
                    var comments = await postService.GetPostCommentsAsync(postId);
                    return Results.Ok(comments);
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(ex.Message);
                }
            });

            routes.MapPost("/api/posts/{postId}/comment",
                [Authorize] async (Guid postId, string content, HttpContext httpContext, IPostService postService) =>
                {
                    var userName = httpContext.User.Identity?.Name;
                    try
                    {
                        var comment = await postService.AddCommentAsync(postId, content, userName);
                        return Results.Ok(comment);
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                }).DisableAntiforgery();

            routes.MapDelete("/api/posts/{postId}/comments/{commentId}",
                [Authorize] async (Guid postId, Guid commentId, HttpContext httpContext, IPostService postService) =>
                {
                    var userName = httpContext.User.Identity?.Name;
                    try
                    {
                        await postService.DeleteCommentAsync(postId, commentId, userName);
                        return Results.Ok();
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return Results.Forbid();
                    }
                });
        }
    }
}
