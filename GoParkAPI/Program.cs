using GoParkAPI;
using GoParkAPI.Controllers;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using GoParkAPI.Services;
using GoParkAPI.Services.Domain;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Configuration;
using Microsoft.Azure.SignalR;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EasyParkContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EasyPark"));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("RedisConnection");
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddHttpClient();

//string PolicyName = "EasyParkCors";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // 第三個網址是使用 ngrok 生成的外部可訪問網址(前端部分)/ by.shan shan
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5173", "http://www.mygoparking.com", "https://www.mygoparking.com", "https://black-ground-0fa128d00.4.azurestaticapps.net", "https://mygoparking.com", "https://sandbox-web-pay.line.me", "https://sandbox-api-pay.line.me").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});



// CORS
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policy =>
//    {
//        policy.WithOrigins("*")
//              .AllowAnyMethod()
//              .AllowAnyHeader().AllowCredentials();
//    });
//});
//----------------------------------------


// 註冊 JsonProvider 作為 Singleton 服務
builder.Services.AddSingleton<JsonProvider>();
// 註冊 LinePayService 並使用 IHttpClientFactory
builder.Services.AddHttpClient<LinePayService>();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<MyPayService>();
builder.Services.AddScoped<ECService>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration); // 注入 IConfiguration


//----------------------------------------


builder.Services.AddScoped<pwdHash>();
builder.Services.AddScoped<MailService>();
builder.Services.AddScoped<MonRentalService>();


//-------------Line Bot-------------------
//註冊Line Bot Service
builder.Services.AddScoped<LineBotService>();
//將HttpClient，注入到 LineBotService 中使用
builder.Services.AddHttpClient<LineBotService>();

// 配置 Hangfire，並設置使用 SQL Server 作為儲存
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseDefaultTypeSerializer()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("EasyPark"), new SqlServerStorageOptions
          {
              CommandBatchMaxTimeout = TimeSpan.FromMinutes(30),
              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(35),
              QueuePollInterval = TimeSpan.Zero,
              UseRecommendedIsolationLevel = true,
              DisableGlobalLocks = true,
              PrepareSchemaIfNecessary = true
          });
});

// 啟用 Hangfire 服務
builder.Services.AddHangfireServer();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>(); // 註冊 CustomUserIdProvider
//// 註冊 ReservationHub
//builder.Services.AddSingleton<ReservationHub>();

//VAPID設置
var vapidConfig = new VapidConfig(
  publicKey: "BEOC-kXHgoTOx9oB89JAGbgZxr2w_IXEc_G4_0PACRCJOFtfx4hoT0hxslv1aGGmCSbrzpV-NSexuMjYuCyoMAM",
  privateKey: "MHnqygHOGLthp9ydqXz6r7Lpmpy1ZdlqzkMHFq0tI80", subject: "mailto:hungkaojay@gmail.com");
builder.Services.AddSingleton(vapidConfig); //注入 VapidConfig
builder.Services.AddScoped<PushNotificationService>(); //注入推播

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]!);

var app = builder.Build();
// 在應用啟動時設置 Recurring Job
//RecurringJob.AddOrUpdate<PushNotificationService>("CheckAndSendOverdueReminder", service => service.CheckAndSendOverdueReminder(), "*/2 * * * *"); // 每隔5分鐘執行一次
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//}
app.UseSwagger();
app.UseSwaggerUI();

//app.MapHub<ReservationHub>("/reservationHub"); // 設置 SignalR Hub 路徑
app.UseRouting();
app.UseCors();



// 啟用 Hangfire Dashboard
//app.UseHangfireDashboard();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAnonymousAuthorization() }
});
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ReservationHub>("/reservationHub");
});

//CROS
//app.UseCors("AllowLocalhost");
app.MapControllers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
