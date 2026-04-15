using System.Text;
using DotNetEnv;
using FesbHelpdesk.Api.Data;
using FesbHelpdesk.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

foreach (var (key, value) in new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = Environment.GetEnvironmentVariable("CONNECTION_STRING"),
    ["Jwt:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET"),
    ["Gemini:ApiKey"] = Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
    ["Smtp:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST"),
    ["Smtp:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT"),
    ["Smtp:Username"] = Environment.GetEnvironmentVariable("SMTP_USERNAME"),
    ["Smtp:Password"] = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
    ["Smtp:FromEmail"] = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL"),
    ["Smtp:FromName"] = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")
})
{
    if (!string.IsNullOrWhiteSpace(value))
        builder.Configuration[key] = value;
}

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Server=localhost;Database=FesbHelpdesk;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHttpClient<IGeminiCategoryService, GeminiCategoryService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
});

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT_SECRET is not configured. Set it in .env or appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FesbHelpdesk",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FesbHelpdesk",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
