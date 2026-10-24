var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.Path = "/swagger";
        options.DocumentPath = "/openapi/v1.json";
        options.SwaggerRoutes.Add(new NSwag.AspNetCore.SwaggerUiRoute("v1", "/openapi/v1.json"));
    });
}

app.MapControllers();

app.Run();

public partial class Program;
