using Healthcare.Server.Services;
using Healthcare.Server.SupabaseIntegration;
using Healthcare.Server.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Healthcare.Server.Services;
using Supabase;
var builder = WebApplication.CreateBuilder(args);

// DotNetEnv.Env.Load(); // Đã chuyển sang appsettings.json
// builder.Configuration.AddEnvironmentVariables();
// 1. Khai báo các Controller API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var supabaseUrl = builder.Configuration["Supabase:Url"]
                  ?? throw new InvalidOperationException("CRITICAL ERROR: Thiếu config Supabase:Url trong appsettings.json!");

var supabaseKey = builder.Configuration["Supabase:ServiceRoleKey"]
                  ?? throw new InvalidOperationException("CRITICAL ERROR: Thiếu config Supabase:ServiceRoleKey trong appsettings.json!");

var options = new SupabaseOptions { AutoConnectRealtime = true };

builder.Services.AddScoped<Supabase.Client>(_ => new Supabase.Client(supabaseUrl, supabaseKey, options));
// 2. Đăng ký các Service 


builder.Services.AddSingleton<SupabaseAdminHelper>();
builder.Services.AddScoped<AiPrescriptionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<SupabaseAdminService>();




// 3. Đăng ký chạy ngầm 
builder.Services.AddHostedService<ScheduledWorker>();
builder.Services.AddControllers();
var app = builder.Build();

// 4. Khởi tạo kết nối Supabase quyền Admin ngay khi Server chạy
var supabaseAdmin = app.Services.GetRequiredService<SupabaseAdminHelper>();
await supabaseAdmin.InitializeAsync();

// 5. Cấu hình HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();