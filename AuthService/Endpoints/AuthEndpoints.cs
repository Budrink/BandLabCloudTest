using AuthService.Services;

namespace AuthService.Endpoints
{
    public static class AuthEndpoints
    {
        // Fake service just for better testing
        public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/api/healthchek", () => Results.Ok("Healthy!"));

            routes.MapPost("/api/auth/login", (TokenService tokenService, string username) =>
            {
                var token = tokenService.GenerateToken(username);
                return Results.Ok(new { Token = token });
            });
        }
    }
}
