using Labs.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ÂÑÅ ðåãèñòðàöèè ñåðâèñîâ ÄÎ Build()
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

// ÊÎÍÔÈÃÓÐÀÖÈß CORS (ÊÐÈÒÈ×ÅÑÊÈ ÂÀÆÍÎ äëÿ Blazor)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp",
        policy =>
        {
            // Ðàçðåøàåì çàïðîñû îò àäðåñà, íà êîòîðîì ðàáîòàåò âàøå Blazor-ïðèëîæåíèå
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

// 2. ÒÎËÜÊÎ ÏÎÑËÅ ÂÑÅÕ Add... âûçûâàåì Build()
var app = builder.Build();

// 3. Êîíôèãóðàöèÿ êîíâåéåðà çàïðîñîâ (Middleware)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ÀÊÒÈÂÀÖÈß ïîëèòèêè CORS (äîëæíî ñòîÿòü äî UseAuthorization è MapControllers)
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
