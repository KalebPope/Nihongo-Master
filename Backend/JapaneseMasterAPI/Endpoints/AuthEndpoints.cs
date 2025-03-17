using JapaneseMasterAPI.Entities;
using JapaneseMasterAPI.Models;
using Microsoft.AspNetCore.Identity;
using JapaneseMasterAPI.Services;
using JapaneseMasterAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace JapaneseMasterAPI.Endpoints
{
    public static class AuthEndpoints

    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("api/auth/signup", Signup);

            app.MapPost("api/auth/login", Login);

            app.MapPost("api/auth/refresh-token", RefreshTokenRequest);

            app.MapGet("api/auth/verify", AuthOnly);
            
        }

        private static async Task<IResult> Signup(JMDbContext context,  UserDto request)

        {
            if (await context.Users.AnyAsync(u => u.UserName == request.Username))
            {
                return Results.BadRequest("Username already exists");
            }

            var user = new User();
            var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);

            user.UserName = request.Username;
            user.PasswordHash = hashedPassword;

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return Results.Ok(user);
        }

        private static async Task<IResult> Login(JMDbContext context, UserDto request, TokenService tokenService)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == request.Username);
            if (user == null)
            {
                return Results.BadRequest("User not found");
            }

            var verifyHash = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (verifyHash == PasswordVerificationResult.Failed)
            {
                return Results.BadRequest("Password is incorrect");
            }

            var response = new TokenResponseDto
            {
                AccessToken = tokenService.CreateToken(user),
                RefreshToken = await tokenService.SaveRefreshToken(user, context)
            };

            return Results.Ok(response);
        }

        private static async Task<IResult> RefreshTokenRequest(JMDbContext context, RefreshTokenDto request, TokenService tokenService)
        {
            var response = await tokenService.RefreshToken(request, context);

            if (response == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(response);
        }

        private static IResult AuthOnly()
        {
            return Results.Ok("Authenticated");
        }

    }
}
