using Postgrest.Attributes;
using Postgrest.Models;

namespace Healthcare.Client.Models.Identity
{
    [Table("admin_profiles")]
    public class AdminProfile : BaseModel
    {
        [PrimaryKey("admin_id", false)] public string AdminId { get; set; }

        [Column("employee_code")] public string EmployeeCode { get; set; }

        [Column("department")] public string Department { get; set; }

        [Column("access_level")] public string AccessLevel { get; set; }
    }
}