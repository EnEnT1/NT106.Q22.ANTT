using Supabase;
using System;
using System.Threading.Tasks;

namespace Healthcare.Server.SupabaseIntegration
{
    public class SupabaseAdminHelper
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        public Supabase.Client AdminClient { get; private set; }

        public SupabaseAdminHelper(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration;
            var url = _configuration["Supabase:Url"]
                      ?? throw new Exception("Thiếu Supabase:Url trong appsettings.json");

            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"]
                                 ?? throw new Exception("Thiếu Supabase:ServiceRoleKey trong appsettings.json");

            var options = new SupabaseOptions { AutoConnectRealtime = false };

            AdminClient = new Supabase.Client(url, serviceRoleKey, options);
        }
        public async Task InitializeAsync()
        {
            await AdminClient.InitializeAsync();
        }
    }
}