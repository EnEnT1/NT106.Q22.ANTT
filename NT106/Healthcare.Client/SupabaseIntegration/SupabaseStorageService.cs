using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Healthcare.Client.SupabaseIntegration
{
    /// <summary>
    /// Service chuyên quản lý việc Tải lên (Upload), Tải xuống (Download) và Xóa file tĩnh
    /// từ các Bucket (Xô chứa) trên Supabase Storage.
    /// </summary>
    public static class SupabaseStorageService
    {
        /// <summary>
        /// Tải một file từ ổ cứng máy tính lên Supabase Storage
        /// </summary>
        /// <param name="bucketName">Tên bucket (ví dụ: "lab_results", "avatars")</param>
        /// <param name="localFilePath">Đường dẫn file trên máy tính (VD: "C:\Images\xray.png")</param>
        /// <param name="supabasePath">Đường dẫn ảo trên Supabase (VD: "patient_123/xray_lan_1.png")</param>
        /// <returns>URL công khai của file sau khi upload thành công</returns>
        public static async Task<string> UploadFileAsync(string bucketName, string localFilePath, string supabasePath)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var bucket = client.Storage.From(bucketName);

                // Upload file lên Supabase (Upsert = true nghĩa là nếu file trùng tên sẽ ghi đè file mới)
                await bucket.Upload(localFilePath, supabasePath, new Supabase.Storage.FileOptions { Upsert = true });

                // Trả về Public URL để lưu thẳng vào Database (cột file_url)
                return bucket.GetPublicUrl(supabasePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi upload file: {ex.Message}");
            }
        }

        /// <summary>
        /// Tải dữ liệu dạng Byte Array (Mảng byte) lên Supabase Storage.
        /// Hữu ích khi bạn chụp ảnh từ Camera hoặc nén ảnh trực tiếp trên RAM mà không lưu xuống ổ cứng.
        /// </summary>
        public static async Task<string> UploadByteArrayAsync(string bucketName, byte[] fileBytes, string supabasePath)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var bucket = client.Storage.From(bucketName);

                await bucket.Upload(fileBytes, supabasePath, new Supabase.Storage.FileOptions { Upsert = true });

                return bucket.GetPublicUrl(supabasePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi upload dữ liệu bộ nhớ: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa file khỏi Supabase Storage
        /// </summary>
        public static async Task<bool> DeleteFileAsync(string bucketName, string supabasePath)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var bucket = client.Storage.From(bucketName);

                // Xóa file theo danh sách đường dẫn
                await bucket.Remove(new List<string> { supabasePath });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi xóa file Storage: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy đường dẫn công khai (Public URL) của một file đã tồn tại trên Supabase
        /// </summary>
        public static string GetPublicUrl(string bucketName, string supabasePath)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                return client.Storage.From(bucketName).GetPublicUrl(supabasePath);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}