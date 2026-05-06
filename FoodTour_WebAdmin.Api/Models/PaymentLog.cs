using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodTour_WebAdmin.Api.Models;

public class PaymentLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 25000;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string PaymentType { get; set; } = "Basic_Download";

    public string? DeviceId { get; set; }

    public string Note { get; set; } = "Khách tự xác nhận và tải";
}
