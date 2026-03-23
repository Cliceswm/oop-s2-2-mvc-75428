using System.ComponentModel.DataAnnotations;

namespace FoodSafetyTracker.Domain.Entities;

    public class Inspection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PremisesId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Inspection Date")]
        public DateTime InspectionDate { get; set; }

        [Range(0, 100)]
        public int Score { get; set; }

        [Required]
        public string Outcome { get; set; } = string.Empty; // Pass or Fail

        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;

        // Navigation properties
        public Premises? Premises { get; set; }
        public ICollection<FollowUp>? FollowUps { get; set; }
    }
