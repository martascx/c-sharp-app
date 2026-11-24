using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Dtos;
using Buteo.EdgeCollector.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");

        // #1 - POST /api/auth/login -> login do admin; para já usando tabela Client --< renomear para Admin??
        auth.MapPost("/login", async (LoginRequest request, BancoDbContext db, TokenStore tokens) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest(new { error = "Palavra Passe e Nome obrigatórios." });

            var name = request.Name.Trim();
            var client = await db.Clients.FirstOrDefaultAsync(c => c.Name == name && c.IsActive);

            if (client is null || !PasswordMatches(request.Password, client.PasswordHash))
                return Results.Json(new { error = "Credenciais inválidas." }, statusCode: StatusCodes.Status401Unauthorized);

            return Results.Ok(new { token = tokens.Create(client.ClientID), name = client.Name });
        });
    }

    private static bool PasswordMatches(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
