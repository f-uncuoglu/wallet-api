using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wallet.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// --- Data -------------------------------------------------------------------
// The connection string is resolved lazily (when the first DbContext is created)
// so it can be overridden by environment variables or by the test host.
builder.Services.AddDbContext<WalletDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

var app = builder.Build();

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
