using Healthcare.Client.Cryptography;
using Healthcare.Client.Helpers;
using Healthcare.Client.Models.Identity;
using Healthcare.Client.UI.Doctor;
using Healthcare.Client.UI.Patient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;
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
        //Đăng nhập
        public static async Task<(bool Success, string Message, string Role)> SignInAsync(string email, string password)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var session = await client.Auth.SignIn(email, password);

                if (session != null && session.User != null)
                {
                    var userRecord = await client.From<User>().Where(x => x.Id == session.User.Id).Single();

                    if (userRecord != null)
                    {
                        if (string.IsNullOrEmpty(userRecord.PublicKey))
                        {
                            var keyPair = RSAManager.GenerateKeys();

                            string encryptedPrivKey = AESManager.Encrypt(keyPair.PrivateKey, password);

                            userRecord.PublicKey = keyPair.PublicKey;
                            userRecord.EncryptedPrivateKey = encryptedPrivKey;

                            await userRecord.Update<User>();
                        }

                        string rawPrivateKey = AESManager.Decrypt(userRecord.EncryptedPrivateKey, password);

                        SessionStorage.CurrentUser = userRecord;
                        SessionStorage.MyPrivateKey = rawPrivateKey;

                        return (true, "Đăng nhập thành công", userRecord.Role);
                    }
                    else
                    {
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

       //Đăng ký
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
                    Message = "Đăng ký thành công! Bạn có thể đăng nhập.",
                    Role = "Patient",
                    AppUser = appUser
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể đăng ký tài khoản. " + ex.Message);
            }
        }

        //Yêu cầu supabase gửi email chứa otp
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
        //Xác thực otp
        public static async Task VerifyOtpAsync(string email, string otp)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                // Gửi otp lên supabase đê có quyền recovery
                var session = await client.Auth.VerifyOTP(email, otp, Supabase.Gotrue.Constants.EmailOtpType.Recovery);

                if (session == null || session.User == null)
                    throw new Exception("Xác thực thất bại.");
            }
            catch (Exception ex)
            {
                throw new Exception("Mã OTP không đúng hoặc đã hết hạn.");
            }
        }
        //Cập nhật lại mật khẩu
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
        //Đăng xuất
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