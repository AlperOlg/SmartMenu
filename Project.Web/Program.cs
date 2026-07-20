// NOT: Eğer sorun çözülmezse Program.cs içerisine şunu ekleyin:
// options.SignIn.RequireConfirmedAccount = false;
// options.Tokens.ProviderMap[TokenOptions.DefaultEmailProvider] ayarlarının doğru yapıldığından emin olun.
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Project.Business.Abstract;
using Project.Business.Concrete;
using Project.DataAccess;
using Project.DataAccess.Concrete;
using Project.DataAccess.Abstract;
using Microsoft.AspNetCore.Identity;
using Project.Core.Entities;
using Project.Web.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped(typeof(IGenericService<>), typeof(GenericManager<>));

builder.Services.AddScoped<IAccountRepository, EfAccountRepository>();
builder.Services.AddScoped<IAccountService, EfAccountManager>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<IRestaurantRepository, EfRestaurantRepository>();
builder.Services.AddScoped<IRestaurantService, EfRestaurantManager>();

builder.Services.AddScoped<ITableRepository, EfTableRepository>();
builder.Services.AddScoped<ITableService, EfTableManager>();

builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IOrderService, EfOrderManager>();

builder.Services.AddScoped<IGenericRepository<MenuItem>, GenericRepository<MenuItem>>();
builder.Services.AddHostedService<TableReleaseBackgroundService>();

builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<SmartMenuDbContext>()
.AddDefaultTokenProviders();


builder.Services.AddDbContext<SmartMenuDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Semantic Kernel + Ollama (yerel AI)
#pragma warning disable SKEXP0070
builder.Services.AddTransient<Kernel>(_ =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOllamaChatCompletion(
        modelId: "llama3",
        endpoint: new Uri("http://localhost:11434"));
    return kernelBuilder.Build();
});
#pragma warning restore SKEXP0070

builder.Services.AddScoped<IAiService, SemanticKernelAiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

//Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SmartMenuDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);
        await DbSeeder.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı seed edilirken bir hata oluştu.");
    }
}

app.Run();
