using STIN_Burza.Filters;
using STIN_Burza.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

builder.Services.AddSingleton<IMyLogger>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logFilePath = config["Configuration:LogFilePath"];
    return new MyLogger(logFilePath ?? "Logs/log.txt");
});

builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IAlphaVantageDataProvider, AlphaVantageDataProvider>();
builder.Services.AddScoped<IAlphaVantageService, AlphaVantageService>();

builder.Services.AddTransient<IStockFilter, ConsecutiveFallingDaysFilter>();
builder.Services.AddTransient<IStockFilter, PriceDropsInLastWindowFilter>();
builder.Services.AddTransient<IStockFilterManager, StockFilterManager>();

builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Stock}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
