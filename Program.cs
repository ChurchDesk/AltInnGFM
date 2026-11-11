var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog(settings =>
    {
        settings.LogName = "Kirkedata Office";
        settings.SourceName = "DGM";
    });

    builder.Logging.AddFilter<
        Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>(
        level => level >= LogLevel.Information);
}

var app = builder.Build();

// Configure the HTTP request pipeline.

    app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("DMG API starter opp");

app.Run();
