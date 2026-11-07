using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;
using GestaoFrota.Data;
using GestaoFrota.Services;
using QuestPDF.Infrastructure;
QuestPDF.Settings.License = LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IEmailService, EmailService>();
var aiEndpoint = builder.Configuration["AI:Endpoint"] ?? "http://localhost:11434";
var aiModel = builder.Configuration["AI:Model"] ?? "gpt-oss:120b-cloud";
builder.Services.AddChatClient(new OllamaApiClient(new Uri(aiEndpoint), aiModel))
    .UseFunctionInvocation();
builder.Services.AddScoped<FrotaTools>();
builder.Services.AddScoped<AssistenteService>();
var app = builder.Build();
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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var admin = await userManager.FindByEmailAsync("admin@gestaofrota.com");
    if (admin != null && !await userManager.IsInRoleAsync(admin, "Admin"))
        await userManager.AddToRoleAsync(admin, "Admin");
}
app.Run();
