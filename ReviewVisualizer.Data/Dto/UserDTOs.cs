using ReviewVisualizer.Data.Enums;

namespace ReviewVisualizer.Data.Dto
{
    public class UpdateGeneratorModificationsDto
    {
        public int UserId { get; set; }
        public GeneratorModifications Modifications { get; set; }
    }

    public class UpdateSystemRolesDto
    {
        public int UserId { get; set; }
        public SystemRoles Roles { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public SystemRoles SystemRoles { get; set; }
        public GeneratorModifications GeneratorModifications { get; set; }
    }
}
