using CreateAuthomaSchemes;
using GenerativeAI.Models;
using CreateAuthomaSchemes.Pages;
using QuickGraph.Algorithms.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient("GoogleGeminiAI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GoogleGeminiAI:Endpoint"]);
    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["GoogleGeminiAI:AIzaSyD9Yyv6lN8lzTJAecUZ6yBt4-_cxsB0IJo"]}");
});
builder.Services.AddSingleton<GenerateMachineModel>();

builder.Services.Configure<GoogleAiOptions>(
    builder.Configuration.GetSection("GoogleAI"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
