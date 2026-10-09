using EventFlow.Api.Auth;
using EventFlow.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<EventFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EventFlowDb")
        ?? throw new InvalidOperationException(
            "Connection string 'EventFlowDb' was not found.")));
builder.Services.AddScoped<IDemoIdentityResolver, DemoIdentityResolver>();
builder.Services.AddScoped<EventFlowAuthorizationService>();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
