using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Важно для [ForeignKey]

namespace WestMotorsApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        [Display(Name = "Полное имя")]
        public string? FullName { get; set; } // nullable

        [StringLength(50)]
        public string? Position { get; set; } // nullable

        [StringLength(200)]
        public string? ContactInfo { get; set; } // nullable

        public int SoldCarsCount { get; set; } // По умолчанию 0

        // НОВЫЕ СВОЙСТВА: Связь с Client
        [Display(Name = "ID Клиента")]
        public int? ClientId { get; set; } // Делаем его nullable, так как не каждый ApplicationUser обязательно является клиентом

        [ForeignKey("ClientId")]
        public Client? Client { get; set; } // Навигационное свойство к сущности Client (nullable)
    }
}