using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Healthcare.Server.SupabaseIntegration;
using Healthcare.Server.Models.Core;
using System.Linq;

namespace Healthcare.Server.Services
{
    public class ScheduledWorker : BackgroundService
    {
        private readonly SupabaseAdminHelper _adminHelper;

        public ScheduledWorker(SupabaseAdminHelper adminHelper)
        {
            _adminHelper = adminHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[Worker] Đang quét lịch hẹn quá hạn và lịch uống thuốc: {DateTime.Now}");

                try
                {
                    await CheckExpiredAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Worker Error] Lỗi khi quét lịch hẹn quá hạn: {ex.Message}");
                }

                // Cho tiến trình ngủ 30 phút rồi chạy lại
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task CheckExpiredAppointmentsAsync()
        {
            var client = _adminHelper.AdminClient;
            if (client == null) return;

            var response = await client.From<Appointment>().Get();
            var appointments = response.Models ?? new System.Collections.Generic.List<Appointment>();

            var now = DateTime.Now;
            var today = now.Date;
            var currentTime = now.TimeOfDay;

            var expiredList = appointments
                .Where(a => (a.Status == "Confirmed" || a.Status == "Arrived") && 
                            (a.AppointmentDate.Date < today || 
                             (a.AppointmentDate.Date == today && a.EndTime < currentTime)))
                .ToList();

            if (expiredList.Any())
            {
                Console.WriteLine($"[Worker] Tìm thấy {expiredList.Count} lịch hẹn quá hạn. Đang chuyển trạng thái sang Missed...");
                foreach (var appt in expiredList)
                {
                    appt.Status = "Missed";
                    await client.From<Appointment>().Update(appt);
                }
                Console.WriteLine("[Worker] Đã cập nhật xong các lịch hẹn quá hạn.");
            }
        }
    }
}