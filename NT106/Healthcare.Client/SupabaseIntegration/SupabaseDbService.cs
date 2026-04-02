using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Postgrest.Models;

namespace Healthcare.Client.SupabaseIntegration
{
    public static class SupabaseDbService
    {
        /// <summary>
        /// Lấy toàn bộ danh sách dữ liệu từ một bảng
        /// Sử dụng: var list = await SupabaseDbService.GetAllAsync<DoctorProfile>();
        /// </summary>
        public static async Task<List<T>> GetAllAsync<T>() where T : BaseModel, new()
        {
            try
            {
                var response = await SupabaseManager.Instance.Client.From<T>().Get();
                return response.Models;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tải dữ liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Thêm mới một dòng vào Database
        /// </summary>
        public static async Task<T> InsertAsync<T>(T model) where T : BaseModel, new()
        {
            try
            {
                var response = await SupabaseManager.Instance.Client.From<T>().Insert(model);
                return response.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm dữ liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật dữ liệu (Yêu cầu Model phải có [PrimaryKey])
        /// </summary>
        public static async Task<T> UpdateAsync<T>(T model) where T : BaseModel, new()
        {
            try
            {
                var response = await SupabaseManager.Instance.Client.From<T>().Update(model);
                return response.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật dữ liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa dữ liệu (Yêu cầu Model phải có [PrimaryKey])
        /// </summary>
        public static async Task DeleteAsync<T>(T model) where T : BaseModel, new()
        {
            try
            {
                await SupabaseManager.Instance.Client.From<T>().Delete(model);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa dữ liệu: {ex.Message}");
            }
        }
    }
}