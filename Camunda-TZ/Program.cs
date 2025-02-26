using Camunda_TZ.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Zeebe.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IZeebeClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var client = ZeebeClient.Builder()
        .UseGatewayAddress(configuration["Camunda:Gateway"])
        .UsePlainText()
        .Build();

    client
        .TopologyRequest()
        .Send()
        .GetAwaiter()
        .GetResult();

    return client;
});

builder.Services.AddHostedService<CheckNewTicketsJob>();
builder.Services.AddHostedService<DistributionJob>();
builder.Services.AddHostedService<CompleteTicketJob>();
builder.Services.AddHostedService<SendMailJob>();

builder.Services.AddDbContextFactory<AppDbContext>((_, optionsBuilder) =>
{
    optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMinio(config =>
{
    config
        .WithEndpoint(builder.Configuration["Minio:Endpoint"])
        .WithCredentials(builder.Configuration["Minio:AccessKey"], builder.Configuration["Minio:SecretKey"])
        .WithSSL(false)
        .Build();
});

IdentityModelEventSource.ShowPII = true;

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Cookies";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.Domain = "144.91.122.220";
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.ClientId = builder.Configuration["Keycloak:ClientId"];
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.CallbackPath = "/signin-oidc";
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.Events = new OpenIdConnectEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.Redirect("/Home/Error");
                context.HandleResponse();
                return Task.CompletedTask;
            },
            OnRedirectToIdentityProvider = context =>
            {
                context.HttpContext.Response.Cookies.Delete(".AspNetCore.Correlation.oidc");
                context.HttpContext.Response.Cookies.Delete(".AspNetCore.OpenIdConnect.Nonce");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

var app = builder.Build();

var db = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
db.Database.Migrate();
db.Dispose();

app.UseExceptionHandler("/Home/Error");
app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();