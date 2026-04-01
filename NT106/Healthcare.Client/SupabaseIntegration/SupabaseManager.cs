using System;
using System.Threading.Tasks;
using Supabase;

namespace Healthcare.Client.SupabaseIntegration
{
    public class SupabaseManager
    {
        private static SupabaseManager _instance;
        public static SupabaseManager Instance => _instance ??= new SupabaseManager();

        public Supabase.Client Client { get; private set; }

        private SupabaseManager()
        {
            // Dán thẳng URL và ANON KEY của bạn vào đây
            string url = "https://uemhxranzmdvgteiodxf.supabase.co";
            string anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVlbWh4cmFuem1kdmd0ZWlvZHhmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQ3MjA4NDQsImV4cCI6MjA5MDI5Njg0NH0.3PFQQP5rDcbVL784m_8AlA_hccAb-H1CNcla-lPLlWQ";

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };

            Client = new Supabase.Client(url, anonKey, options);
        }

        public async Task InitializeAsync()
        {
            await Client.InitializeAsync();
        }
    }
}