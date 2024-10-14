using easy_a_web_api.Services;

internal class Program
{
    private static void Main(string[] args)
    {
        FireStoreService.SetEnvironmentVariable();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure CORS with specific allowed origins
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins",
                builder => builder.WithOrigins(
                        "https://orange-river-0233d5603.5.azurestaticapps.net", // Your React app's URL
                        "http://localhost:3000" // Local development URL
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        // Use CORS before authorization
        app.UseCors("AllowSpecificOrigins");

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}