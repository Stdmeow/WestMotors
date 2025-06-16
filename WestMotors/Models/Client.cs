using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WestMotorsApp.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ФИО клиента обязательно")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Паспортные данные обязательны")]
        [StringLength(200)]
        public string PassportData { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Контактные данные обязательны")]
        [StringLength(200)]
        public string ContactInfo { get; set; } = string.Empty;

        [Display(Name = "Адрес")]
        [StringLength(500)]
        public string? Address { get; set; }

        public string? Preferences { get; set; }

        public ICollection<Deal>? Deals { get; set; }
        public ICollection<ApplicationRequest>? ApplicationRequests { get; set; }
    }
}