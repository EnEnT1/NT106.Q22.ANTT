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
        public static async Task<(bool Success, string Message, string Role)> SignInAsync(string email, string password)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;

                // 1. Xác thực qua hệ thống Auth của Supabase
                var session = await client.Auth.SignIn(email, password);

                if (session != null && session.User != null)
                {
                    // 2. LẤY ROLE TỪ BẢNG public.users DỰA VÀO ID VỪA ĐĂNG NHẬP
                    var userRecord = await client.From<User>()
                                                 .Where(x => x.Id == session.User.Id)
                                                 .Single();

                    if (userRecord != null)
                    {
                        // Trả về đúng Role đang lưu trong database (Admin, Doctor, hoặc Patient)
                        return (true, "Đăng nhập thành công", userRecord.Role);
                    }
                    else
                    {
                        // Trường hợp Supabase Auth có tài khoản nhưng bảng Users chưa có dữ liệu
                        return (false, "Lỗi: Không tìm thấy hồ sơ người dùng trong hệ thống.", "");
                    }
                }

                return (false, "Sai email hoặc mật khẩu.", "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message, "");
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

                string newUserID = session.User.Id;

                var newUser = new User
                {
                    Id = newUserID,
                    Email = email,
                    FullName = fullName,
                    Phone = phone,
                    Role = "Patient",
                    CreatedAt = DateTime.Now,
                };
                
                var newPatientProfile = new PatientProfile
                {
                    PatientId = newUserID,
                };

                var insertUserTask = client.From<User>().Insert(newUser);
                
                await insertUserTask;

                
                var insertProfileTask = client.From<PatientProfile>().Insert(newPatientProfile);
                await insertProfileTask;

                await Task.WhenAll(insertUserTask, insertProfileTask);
                var appUser = MapToAppUser(session.User, "Patient");
                return new AuthResult
                {
                    Success = true,
                    Message = "Đăng ký thành công! Bạn có thể đăng nhập ngay lập tức.",
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
            try
            {
                var client = SupabaseManager.Instance.Client;

                // Gửi 6 số OTP lên Supabase để xác minh quyền khôi phục (Recovery)
                var session = await client.Auth.VerifyOTP(email, otp, Supabase.Gotrue.Constants.EmailOtpType.Recovery);

                if (session == null || session.User == null)
                    throw new Exception("Xác thực thất bại.");

                // Nếu thành công, Supabase sẽ tự động đăng nhập ngầm user này, 
                // lúc này hàm UpdatePasswordAsync ở Step 3 mới được phép chạy!
            }
            catch (Exception ex)
            {
                throw new Exception("Mã OTP không đúng hoặc đã hết hạn.");
            }
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