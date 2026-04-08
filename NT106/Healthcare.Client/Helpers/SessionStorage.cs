using Healthcare.Client.Models.Identity;

namespace Healthcare.Client.Helpers
{
    public static class SessionStorage
    {
        public static User CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static string MyPrivateKey { get; set; }
        public static void ClearSession()
        {
            CurrentUser = null;
        }
    }
}