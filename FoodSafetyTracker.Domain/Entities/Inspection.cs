using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodSafetyTracker.Domain.Entities;

public class Inspection
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Premises is required")]
    [Display(Name = "Premises")]
    public int PremisesId { get; set; }

    [Required(ErrorMessage = "Inspection date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Inspection Date")]
    public DateTime InspectionDate { get; set; }

    [Required(ErrorMessage = "Score is required")]
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
    [Display(Name = "Score")]
    public int Score { get; set; }

    [Required]
    [Display(Name = "Outcome")]
    public string Outcome { get; set; } = string.Empty; // Pass or Fail - automatically calculated

    [Display(Name = "Notes")]
    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("PremisesId")]
    public virtual Premises? Premises { get; set; }

    public virtual ICollection<FollowUp>? FollowUps { get; set; }
}