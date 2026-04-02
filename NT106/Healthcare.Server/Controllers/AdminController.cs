using Microsoft.AspNetCore.Mvc;
using Healthcare.Server.Services;
using System.Threading.Tasks;

namespace Healthcare.Server.Controllers
{
    
    public class UserReq
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly SupabaseAdminService _adminService;

        // Tiêm Service vào Controller (Dependency Injection)
        public AdminController(SupabaseAdminService adminService)
        {
            _adminService = adminService;
        }

        // API Xóa người dùng: DELETE api/admin/users/{userId}
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> Delete(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest("ID không hợp lệ.");

            var isDeleted = await _adminService.DeleteUserCompletelyAsync(userId);

            if (isDeleted) return Ok(new { message = "Xóa người dùng thành công." });
            return StatusCode(500, new { message = "Lỗi hệ thống khi xóa người dùng. Kiểm tra Console Server để biết chi tiết." });
        }

        // API Thêm người dùng: POST api/admin/users
        [HttpPost("users")]
        public async Task<IActionResult> Create([FromBody] UserReq req)
        {
            if (req == null) return BadRequest("Dữ liệu không hợp lệ.");

            var isCreated = await _adminService.CreateUserCompletelyAsync(req.Email, req.Password, req.FullName, req.Role);

            if (isCreated) return Ok(new { message = "Tạo người dùng thành công." });
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo người dùng." });
        }
    }
}