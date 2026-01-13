using System.ComponentModel.DataAnnotations;

namespace ClientLauncher.Implement.ViewModels.Response
{
    public class AssignRoleToEmployeeRequest
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public List<int> RoleIds { get; set; } = new();
    }
}