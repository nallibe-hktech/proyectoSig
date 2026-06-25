using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SIG.API.Filters;
using SIG.API.Middleware;
using SIG.Application.Interfaces.Services;
using SIG.Application.Validators;
using SIG.Infrastructure;
using SIG.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Quitar el mapeo automático de claims (sub → NameIdentifier).
// Aún así, el código usa ClaimTypes.NameIdentifier que ASP.NET sigue rellenando.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Controllers + JSON enums como strings
builder.Services.AddControllers(opts =>
{
    opts.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SIG · Plataforma de Cierres API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT bearer. Ejemplo: 'Bearer abc.def.ghi'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// CORS para Angular en localhost:4200
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200", "https://thankful-sand-06e86fb03.7.azurestaticapps.net")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// JWT
var jwtKey = builder.Configuration["JwtSettings:SigningKey"]
             ?? throw new InvalidOperationException("JwtSettings:SigningKey no configurada.");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "sig-plataforma";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "sig-plataforma";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Migraciones + seed en Dev/Testing/E2E
using (var scope = app.Services.CreateScope())
{
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    if (env.IsDevelopment() || env.EnvironmentName == "Testing" || env.EnvironmentName == "E2E")
    {
        try
        {
            await db.Database.MigrateAsync();
            if (app.Configuration.GetValue<bool>("Seed:AutoRun"))
            {
                var seeder = scope.ServiceProvider.GetRequiredService<ISeedService>();
                await seeder.RunIfEmptyAsync(CancellationToken.None);
            }
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error aplicando migraciones a la base de datos");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Error en configuración o servicios durante seed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado aplicando migraciones o seed");
        }
    }
}

await app.RunAsync();

public partial class Program { } // NOSONAR — top-level statements

