using System.ComponentModel.DataAnnotations;

namespace FoodSafetyTracker.Web.Models
{
    public class Premises
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Business Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Town { get; set; } = string.Empty;

        [Required]
        public string RiskRating { get; set; } = "Medium"; // Low, Medium, High

        // Navigation property - one Premises has many Inspections
        public ICollection<Inspection>? Inspections { get; set; }
    }
}