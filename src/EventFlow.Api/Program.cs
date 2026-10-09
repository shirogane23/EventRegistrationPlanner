using EventFlow.Api.Auth;
using EventFlow.Api.Data;
using EventFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<INotificationService, LoggingNotificationService>();
builder.Services.AddAutoMapper(typeof(Program).Assembly);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventFlow API",
        Version = "v1",
        Description = "Event registration API. Use Authorize to set X-Demo-Identity."
    });
    options.AddSecurityDefinition("DemoIdentity", new OpenApiSecurityScheme
    {
        Name = DemoIdentityConstants.HeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Enter demo-attendee or demo-organizer.",
        Scheme = "DemoIdentity"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "DemoIdentity"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
