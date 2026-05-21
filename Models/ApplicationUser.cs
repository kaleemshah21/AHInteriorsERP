using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
// This model extends the default Identity user
// by adding optional first name and last name fields.
namespace AHInteriorsERP.Models
{
 
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }
    }
    
}
