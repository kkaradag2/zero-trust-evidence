using System.Text.Json.Serialization;
using Zte.Backend.Application.SoftwareAttestation;
using Zte.Backend.Application.Measurements;
using Zte.Backend.Infrastructure.Measurements;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

builder.Services.AddScoped<ISoftwareAttestationService, SoftwareAttestationService>();
builder.Services.AddSingleton<IMeasurementStore, InMemoryMeasurementStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();