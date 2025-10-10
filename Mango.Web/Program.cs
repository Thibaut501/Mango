using Mango.Web.Service;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Mango.Web.Data;
using Mango.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// Configure HttpClient named "Default"; in Development, bypass dev cert validation
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient("Default")
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
}
else
{
    builder.Services.AddHttpClient("Default");
}

// Base URLs from appsettings.json
SD.ProductAPIBase = builder.Configuration["ServiceUrls:ProductAPI"];
SD.CouponAPIBase = builder.Configuration["ServiceUrls:CouponAPI"];
SD.ShoppingCartAPIBase = builder.Configuration["ServiceUrls:ShoppingCartAPI"];
SD.OrderAPIBase = builder.Configuration["ServiceUrls:OrderAPI"];
SD.AuthAPIBase = builder.Configuration["ServiceUrls:AuthAPI"];

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBaseService, BaseService>();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
builder.Services.AddScoped<IBankingService, BankingService>();

// Banking persistence (local DB for demo). Add connection string Banking in appsettings.json
builder.Services.AddDbContext<BankingDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("BankingConnection"));
});

// Cookie Authentication (for UI)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

var app = builder.Build();

// Apply EF Core migrations automatically at startup (safe for dev/test)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    // Ensure database exists and apply pending migrations
    await db.Database.MigrateAsync();

    // Seed minimal data if empty
    if (!db.Vendors.Any())
    {
        db.Vendors.AddRange(new[]
        {
            new VendorDto { Name = "FreshFoods", Contact = "contact@fresh.com", OutstandingBalance = 120.50m, LastOrderDate = DateTime.UtcNow.AddDays(-2) },
            new VendorDto { Name = "VeggieCo", Contact = "sales@veggieco.com", OutstandingBalance = 0m, LastOrderDate = DateTime.UtcNow.AddDays(-7) }
        });
    }
    if (!db.Expenses.Any())
    {
        db.Expenses.AddRange(new[]
        {
            new ExpenseDto { Category = "Utilities", Description = "Electricity bill", Amount = 210.10m, Date = DateTime.UtcNow.AddDays(-1) },
            new ExpenseDto { Category = "Maintenance", Description = "Oven repair", Amount = 89.99m, Date = DateTime.UtcNow }
        });
    }
    await db.SaveChangesAsync();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Content Security Policy: allow required third-parties (Stripe, CDNs) and hCaptcha domains
app.Use(async (ctx, next) =>
{
    var csp = string.Join("; ", new[]
    {
        "default-src 'self'",
        // Scripts and styles from commonly used CDNs + hCaptcha
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://js.stripe.com https://hcaptcha.com https://*.hcaptcha.com https://newassets.hcaptcha.com https://*.newassets.hcaptcha.com",
        "script-src-elem 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://js.stripe.com https://hcaptcha.com https://*.hcaptcha.com https://newassets.hcaptcha.com https://*.newassets.hcaptcha.com",
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net",
        // Images from self, data URIs, and any HTTPS origin (use scheme source "https:") + hCaptcha
        "img-src 'self' data: https: https://hcaptcha.com https://*.hcaptcha.com",
        // Fonts
        "font-src 'self' data: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net",
        // Frames we need (Stripe + hCaptcha)
        "frame-src https://js.stripe.com https://checkout.stripe.com https://hcaptcha.com https://*.hcaptcha.com",
        // API calls (allow HTTPS and dev websockets) + hCaptcha endpoints
        "connect-src 'self' https: wss: https://hcaptcha.com https://*.hcaptcha.com https://newassets.hcaptcha.com https://*.newassets.hcaptcha.com",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self' https://checkout.stripe.com",
        "frame-ancestors 'self'",
        "upgrade-insecure-requests"
    });

    ctx.Response.Headers["Content-Security-Policy"] = csp;
    await next();
});

app.UseStaticFiles();

app.UseRouting();

// Enable cookie authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Razor Pages route
app.MapRazorPages();

app.Run();
