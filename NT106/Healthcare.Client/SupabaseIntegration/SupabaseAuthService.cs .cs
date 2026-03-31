using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.UI.Doctor;
using Healthcare.Client.UI.Patient;
namespace Healthcare.Client.SupabaseIntegration
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Role { get; set; } = "Patient";
        public User AppUser { get; set; }
    }

    public static class SupabaseAuthService
    {
        public static async Task<AuthResult> SignInAsync(string email, string password)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var session = await client.Auth.SignIn(email, password);

                if (session == null || session.User == null)
                    throw new Exception("Không thể đăng nhập. Phiên đăng nhập không hợp lệ.");

                // Lấy role từ user metadata/app metadata
                string role = GetRoleFromSupabaseUser(session.User);

                // Map sang model User của app
                var appUser = MapToAppUser(session.User, role);

                // Lưu phiên đăng nhập
                SessionStorage.CurrentUser = appUser;

                return new AuthResult
                {
                    Success = true,
                    Message = "Đăng nhập thành công.",
                    Role = role,
                    AppUser = appUser
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Email hoặc mật khẩu không đúng. " + ex.Message);
            }
        }

        // =========================================================
        // Đăng ký
        // Mặc định role = Patient
        // =========================================================
        public static async Task<AuthResult> SignUpAsync(string fullName, string email, string phone, string password)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var options = new Supabase.Gotrue.SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "full_name", fullName },
                         { "phone", phone },
                        { "role", "Patient" }
                     }
                };

                // Supabase C# thường hỗ trợ overload có data / options tùy version.
                // Cách này là best effort cho flow auth chuẩn.
                var session = await client.Auth.SignUp(email, password, options);

                if (session == null || session.User == null)
                    throw new Exception("Không tạo được tài khoản.");

                var appUser = MapToAppUser(session.User, "Patient");

                return new AuthResult
                {
                    Success = true,
                    Message = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận.",
                    Role = "Patient",
                    AppUser = appUser
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể đăng ký tài khoản. " + ex.Message);
            }
        }

        // =========================================================
        // Gửi email reset password
        // Dùng cho ForgotPassword Step 1
        // =========================================================
        public static async Task SendResetPasswordEmailAsync(string email)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                await client.Auth.ResetPasswordForEmail(email);
            }
            catch (Exception ex)
            {
                throw new Exception("Không gửi được email khôi phục mật khẩu. " + ex.Message);
            }
        }

        // =========================================================
        // Verify OTP
        // Hiện tại Supabase reset password chuẩn qua email link.
        // Nếu UI bạn vẫn muốn giữ step OTP 6 số, tạm cho pass qua bước 2
        // hoặc sau này thay bằng OTP provider thật.
        // =========================================================
        public static async Task VerifyOtpAsync(string email, string otp)
        {
            await Task.Delay(300);

            if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6)
                throw new Exception("Mã OTP không hợp lệ.");

            // TODO thật sự:
            // Nếu dùng OTP SMS/email code thật, thay chỗ này bằng API verify thật.
            // Hiện tại chỉ giữ flow UI 3 bước để app chạy được.
        }

        // =========================================================
        // Cập nhật mật khẩu mới
        // Chỉ chạy được khi user đã ở trạng thái recovery/login hợp lệ
        // =========================================================
        public static async Task UpdatePasswordAsync(string newPassword)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                var attrs = new Supabase.Gotrue.UserAttributes
                {
                    Password = newPassword
                };

                await client.Auth.Update(attrs);

                await client.Auth.Update(attrs);
            }
            catch (Exception ex)
            {
                throw new Exception("Không cập nhật được mật khẩu. " + ex.Message);
            }
        }

        // =========================================================
        // Đăng xuất
        // =========================================================
        public static async Task SignOutAsync()
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                await client.Auth.SignOut();
                SessionStorage.ClearSession();
            }
            catch
            {
                SessionStorage.ClearSession();
            }
        }

        // =========================================================
        // Lấy role từ metadata
        // Ưu tiên: user_metadata.role -> app_metadata.role -> Patient
        // =========================================================
        private static string GetRoleFromSupabaseUser(dynamic sbUser)
        {
            try
            {
                if (sbUser?.UserMetadata != null)
                {
                    if (sbUser.UserMetadata.ContainsKey("role"))
                        return sbUser.UserMetadata["role"]?.ToString() ?? "Patient";
                }
            }
            catch { }

            try
            {
                if (sbUser?.AppMetadata != null)
                {
                    if (sbUser.AppMetadata.ContainsKey("role"))
                        return sbUser.AppMetadata["role"]?.ToString() ?? "Patient";
                }
            }
            catch { }

            return "Patient";
        }

        // =========================================================
        // Map user Supabase -> User của app
        // Lưu ý:
        // Bạn phải chỉnh tên thuộc tính cho khớp class User thật nếu khác
        // =========================================================
        private static User MapToAppUser(dynamic sbUser, string role)
        {
            var user = new User();

            try { user.Id = sbUser.Id; } catch { }
            try { user.Email = sbUser.Email; } catch { }

            try
            {
                if (sbUser?.UserMetadata != null)
                {
                    if (sbUser.UserMetadata.ContainsKey("full_name"))
                        user.FullName = sbUser.UserMetadata["full_name"]?.ToString();

                    if (sbUser.UserMetadata.ContainsKey("phone"))
                        user.Phone = sbUser.UserMetadata["phone"]?.ToString();
                }
            }
            catch { }

            try { user.Role = role; } catch { }

            return user;
        }
    }
}