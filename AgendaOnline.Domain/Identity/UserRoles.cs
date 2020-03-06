using Microsoft.AspNetCore.Identity;

namespace AgendaOnline.Domain.Identity
{
    public class UserRole : IdentityUserRole<int>
    {
        public User User { get; set; }
        public Role Role { get; set; }
    }
}