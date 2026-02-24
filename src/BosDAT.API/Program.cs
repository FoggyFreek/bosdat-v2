using BosDAT.API.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddIdentityConfiguration()
    .AddJwtAuthentication(builder.Configuration)
    .AddApplicationServices()
    .AddCorsPolicy(builder.Configuration)
    .AddSwaggerDocumentation();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new BosDAT.API.Converters.TimeOnlyJsonConverter());
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.InitializeDatabaseAsync();

await app.RunAsync();
