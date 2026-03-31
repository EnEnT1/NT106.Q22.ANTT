using Healthcare.Server.Services;
using Healthcare.Server.SupabaseIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();
// 1. Khai báo các Controller API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

// 2. Đăng ký các Service (Dependency Injection)
builder.Services.AddSingleton<SupabaseAdminHelper>();
builder.Services.AddScoped<AiPrescriptionService>();
builder.Services.AddScoped<PaymentService>();

// 3. Đăng ký chạy ngầm (Background Service)
builder.Services.AddHostedService<ScheduledWorker>();

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();