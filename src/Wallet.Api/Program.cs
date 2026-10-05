using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wallet.Api.Data;
using Wallet.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- HTTP layer -------------------------------------------------------------
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Every error leaves the API as an RFC 7807 ProblemDetails document.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// --- Data -------------------------------------------------------------------
// The connection string is resolved lazily (when the first DbContext is created)
// so it can be overridden by environment variables or by the test host.
builder.Services.AddDbContext<WalletDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

var app = builder.Build();

// Order matters: the exception handler wraps everything registered after it.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
