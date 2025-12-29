using Labs.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ВСЕ регистрации сервисов до Build()

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("LabsDb"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// КОНФИГУРАЦИЯ CORS (КРИТИЧЕСКИ ВАЖНО для Blazor)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp",
        policy =>
        {
            // Разрешаем запросы от адреса, на котором работает ваше Blazor-приложение
            policy.WithOrigins("https://localhost:7002",
                               "https://localhost:7003",
                               "http://localhost:5002",
                               "http://localhost:5003", 
                               "https://localhost:5001",  
                               "http://localhost:5000")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// 2. После всех Add... вызываем Build()

var app = builder.Build();

// 3. Конфигурация конвейера запросов (Middleware)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// АКТИВАЦИЯ политики CORS (должно стоять до UseAuthorization и MapControllers)

app.UseCors("AllowALL");

app.UseAuthorization();
app.MapControllers();
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});
//Инициализация БД
await DbInitializer.SeedData(app);

app.Run();
