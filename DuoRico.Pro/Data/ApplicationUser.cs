using Microsoft.AspNetCore.Identity;

namespace DuoRico.Pro.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public required string Name { get; set; }

        [PersonalData]
        public Guid? CoupleId { get; set; }
    }

}
