using System.Text.Json.Serialization;
using Zte.Backend.Application.Challenges;
using Zte.Backend.Application.HardwareAttestation;
using Zte.Backend.Application.Measurements;
using Zte.Backend.Application.SoftwareAttestation;
using Zte.Backend.Infrastructure.Challenges;
using Zte.Backend.Infrastructure.HardwareAttestation;
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
builder.Services.AddSingleton<IChallengeStore, InMemoryChallengeStore>();
builder.Services.AddSingleton<IRegisteredDeviceKeyStore, InMemoryRegisteredDeviceKeyStore>();
builder.Services.AddScoped<IHardwareAttestationService, HardwareAttestationService>();
builder.Services.AddSingleton<IRegisteredDeviceKeyStore, InMemoryRegisteredDeviceKeyStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();