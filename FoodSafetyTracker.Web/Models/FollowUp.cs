using System.ComponentModel.DataAnnotations;

namespace FoodSafetyTracker.Web.Models
{
    public class FollowUp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InspectionId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]
        public string Status { get; set; } = "Open"; // Open or Closed

        [DataType(DataType.Date)]
        [Display(Name = "Closed Date")]
        public DateTime? ClosedDate { get; set; }

        // Navigation property
        public Inspection? Inspection { get; set; }
    }
}