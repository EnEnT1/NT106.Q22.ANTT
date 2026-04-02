using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Healthcare.Server.Services
{
    public class ScheduledWorker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[Worker] Đang quet lich uong thuoc: {DateTime.Now}");

                // TODO: Gọi Supabase lấy danh sách MedicationReminder
                // Nếu giờ hiện tại trùng với giờ uống thuốc -> Đẩy 1 dòng vào bảng Notification

                // Cho tiến trình ngủ 30 phút rồi chạy lại
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}