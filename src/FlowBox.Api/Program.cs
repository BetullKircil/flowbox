using System.Reflection;
using FlowBox.Api.Data.Ef;
using FlowBox.Api.Endpoints;
using FlowBox.Api.Registry;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFlowBoxDbContext(builder.Configuration);

// Data katmanı: bugün EF Core, yarın istersen Dapper — Service/Endpoint katmanları
// sadece IShipmentRepository/ICourierRepository'yi bilir, implementasyonu değil.
builder.Services.AddRepositories();

// Business (Service) katmanı: iş kuralları burada, HTTP'den habersiz.
builder.Services.AddService();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// IEndpoint implementasyonlarını burada bir kere reflection ile buluyoruz;
// MapEndpoints bu listeyi parametre olarak alır (Registry/EndpointRouteBuilderExtensions).
var endpointTypes = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
    .ToList();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.UseHttpsRedirection();

app.MapEndpoints(endpointTypes);

// Uygulama başlarken bekleyen tüm migration'ları veritabanına uygular
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlowBoxDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program { }
