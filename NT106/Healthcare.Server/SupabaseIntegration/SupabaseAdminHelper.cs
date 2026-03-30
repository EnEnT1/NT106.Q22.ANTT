using Supabase;
using System;
using System.Threading.Tasks;

namespace Healthcare.Server.SupabaseIntegration
{
    public class SupabaseAdminHelper
    {
        public Supabase.Client AdminClient { get; private set; }

        public SupabaseAdminHelper()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL")
                      ?? throw new Exception("Thiếu SUPABASE_URL trong .env");

            var serviceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
                                 ?? throw new Exception("Thiếu SUPABASE_SERVICE_ROLE_KEY trong .env");

            var options = new SupabaseOptions { AutoConnectRealtime = false };

            AdminClient = new Supabase.Client(url, serviceRoleKey, options);
        }
        public async Task InitializeAsync()
        {
            await AdminClient.InitializeAsync();
        }
    }
}