using Verbraucher.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Verbraucher.Entities;
using Verbraucher.Persistence;
using Verbraucher.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("E:\\Lager\\secrets.json");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<Repository>();
builder.Services.AddScoped<PdfService>();

// https://css-tricks.com/snippets/css/a-guide-to-flexbox/
builder.Services
    .AddDbContextFactory<VerbraucherContext>(options => options.UseMySql(builder.Configuration.GetConnectionString("mariaDb"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mariaDb")))
    .EnableSensitiveDataLogging()
    .EnableDetailedErrors());


//services.AddDbContext<HausdatenContext>((options => options.UseMySql(Configuration.GetConnectionString("mariaDb"), ServerVersion.AutoDetect(Configuration.GetConnectionString("mariaDb")))));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
