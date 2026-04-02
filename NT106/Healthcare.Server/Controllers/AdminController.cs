using Microsoft.AspNetCore.Mvc;
using Healthcare.Server.Services;
using System.Threading.Tasks;

namespace Healthcare.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly SupabaseAdminService _adminService;

        public AdminController(SupabaseAdminService adminService)
        {
            _adminService = adminService;
        }

        // Endpoint: DELETE https://localhost:xxxx/api/admin/users/{userId}
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "ID người dùng không hợp lệ." });
            }

            var isDeleted = await _adminService.DeleteUserCompletelyAsync(userId);

            if (isDeleted)
                return Ok(new { message = "Xóa người dùng thành công." });
            else
                return StatusCode(500, new { message = "Lỗi hệ thống khi xóa người dùng trên Supabase." });
        }
    }
}