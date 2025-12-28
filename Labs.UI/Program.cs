using Labs.UI.Data;
using Labs.UI.Services;
using Labs.UI.Services.Contracts;
using Labs.UI.Extensions;
using Labs.UI.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    // Add services to the container.
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("LabsIdentityDb"));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddDefaultIdentity<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

    // Регистрация API сервисов
    builder.Services.AddHttpClient<ICategoryService, ApiCategoryService>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7002/api/categories/");
    });

    builder.Services.AddHttpClient<IProductService, ApiProductService>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7002/api/dishes/");
    });

    // Для тэг-хелперов
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddAuthorization(opt =>
    {
        opt.AddPolicy("admin", p =>
            p.RequireClaim(System.Security.Claims.ClaimTypes.Role, "admin"));
    });

    builder.Services.AddSingleton<IEmailSender, NoOpEmailSender>();

    // Добавление сессий
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    // Настройка HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();
    app.UseFileLogger();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "catalog",
        pattern: "Catalog/{category?}/{pageNo?}",
        defaults: new { controller = "Product", action = "Index" });

    app.MapControllerRoute(
        name: "image",
        pattern: "image/{action=GetAvatar}",
        defaults: new { controller = "Image" });

    app.MapRazorPages();

    // Создаем базу данных и администратора
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        await DbInit.SetupIdentityAdmin(userManager);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
