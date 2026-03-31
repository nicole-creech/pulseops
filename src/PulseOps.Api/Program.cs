using Microsoft.EntityFrameworkCore;
using PulseOps.Domain.Entities;
using PulseOps.Infrastructure.Persistence;
using PulseOps.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<EventPublisher>();
builder.Services.AddScoped<IdempotencyService>();

builder.Services.AddDbContext<PulseOpsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PulseOpsDb")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "PulseOps.Api",
    utc = DateTime.UtcNow
}));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PulseOpsDbContext>();

    if (!db.Businesses.Any())
    {
        db.Businesses.Add(new Business
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Demo Business",
            CreatedAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}

app.Run();