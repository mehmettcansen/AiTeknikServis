using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AiTeknikServis.Infrastructure.Extensions;
using AiTeknikServis.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Entity Framework Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity Configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders(); // Bu satırı ekledik

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(Program));

// Custom Service Extensions
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var userRepository = scope.ServiceProvider.GetRequiredService<AiTeknikServis.Repositories.Contracts.IUserRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    string[] roles = { "Admin", "Manager", "Technician", "Customer" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
    
    // Create default admin user
    var adminEmail = "admin@aiteknikservis.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    
    // Fix existing customers without IdentityUserId
    try
    {
        var allCustomers = await userRepository.GetAllCustomersAsync();
        var customersWithoutIdentity = allCustomers.Where(c => string.IsNullOrEmpty(c.IdentityUserId)).ToList();
        
        foreach (var customer in customersWithoutIdentity)
        {
            var identityUser = await userManager.FindByEmailAsync(customer.Email);
            if (identityUser != null)
            {
                customer.IdentityUserId = identityUser.Id;
                await userRepository.UpdateCustomerAsync(customer);
                logger.LogInformation("Fixed IdentityUserId for customer: {Email}", customer.Email);
            }
        }
        
        if (customersWithoutIdentity.Any())
        {
            logger.LogInformation("Fixed {Count} customers without IdentityUserId", customersWithoutIdentity.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fixing customer IdentityUserId links");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Area routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
