using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DoAnLTW.Models;
using DoAnLTW.Models.Repositories;
using DoAnLTW.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using DoAnLTW.Models.Momo;
using DoAnLTW.Services.Momo;
using DoAnLTW.Repositories;
using DoAnLTW.Hubs;
using Serilog;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddScoped<IPetServiceRepository, PetServiceRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<IHotelRoomRepository, HotelRoomRepository>();
builder.Services.AddScoped<IPetHotelBookingRepository, PetHotelBookingRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cấu hình cookie xác thực
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
});

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("http://localhost:5134", "https://localhost:5134")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Cấu hình logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddSerilog();
});

// Đăng ký Serilog.Extensions.Hosting
builder.Host.UseSerilog();

// Razor view render
builder.Services.AddTransient<IRazorViewToStringRenderer, RazorViewToStringRenderer>();

// Momo
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();

// Token password reset
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromSeconds(30);
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.EnableDetailedErrors = true;
});

// Email
builder.Services.AddTransient<IEmailSender, SendMail>();

// Repository
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

try
{
    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseStaticFiles();
    app.UseSession();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseCookiePolicy();
    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseRouting();
    app.UseAuthentication();

    // Middleware kiểm tra vai trò và chuyển hướng
    app.Use(async (context, next) =>
    {
        var user = context.User;
        if (user.Identity.IsAuthenticated)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
            var currentUser = await userManager.GetUserAsync(user);
            if (currentUser != null)
            {
                var roles = await userManager.GetRolesAsync(currentUser);
                if (roles.Contains("Admin") || roles.Contains("Employee"))
                {
                    // Nếu là Admin hoặc Employee và đang cố truy cập Home/Index, chuyển hướng
                    var path = context.Request.Path.Value?.ToLower();
                    if (path == "/" || path.StartsWith("/home") || path.StartsWith("/index"))
                    {
                        context.Response.Redirect("/Admin/Statistics");
                        return;
                    }
                }
            }
        }
        await next();
    });

    app.UseAuthorization();

    app.MapRazorPages();
    app.MapHub<ChatHub>("/chatHub");

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}