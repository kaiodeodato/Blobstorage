using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;
using BlobStorage.Services;
using BlobStorage.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddSingleton<IBlobStorageClientFactory, BlobStorageClientFactory>();
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<IBlobRepository, BlobRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.UseRouting();

app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/api/v1.0/Home/"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
