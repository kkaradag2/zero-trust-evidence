using System.Text.Json.Serialization;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Application.Features.HardwareAttestation.Services;
using Zte.Backend.Application.Features.SoftwareAttestation.Services;
using Zte.Backend.Infrastructure.Persistence.Benchmarks;
using Zte.Backend.Infrastructure.Persistence.Challenges;
using Zte.Backend.Infrastructure.Persistence.HardwareAttestation;
using Zte.Backend.Infrastructure.Persistence.Measurements;

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
builder.Services.AddSingleton<IEnrolledDeviceStore, InMemoryEnrolledDeviceStore>();
builder.Services.AddSingleton<IRegisteredDeviceKeyStore, InMemoryRegisteredDeviceKeyStore>();
builder.Services.AddScoped<IHardwareAttestationEnrollmentService, HardwareAttestationEnrollmentService>();
builder.Services.AddScoped<IHardwareAttestationService, HardwareAttestationService>();

builder.Services.AddSingleton<IBenchmarkRunStore, InMemoryBenchmarkRunStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseDefaultFiles(new DefaultFilesOptions
{
    RequestPath = "/dashboard",
    DefaultFileNames = { "index.html" }
});

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/dashboard/{*path:nonfile}", "dashboard/index.html");

app.Run();
