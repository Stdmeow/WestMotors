using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WestMotorsApp.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название поставщика обязательно")]
        [StringLength(100)]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Контактные данные обязательны")]
        [StringLength(200)]
        [Display(Name = "Контактные данные")]
        public string ContactInfo { get; set; }

        [StringLength(200)]
        [Display(Name = "Банковские реквизиты")]
        public string? BankDetails { get; set; }

        public string? History { get; set; } // История поставок
    }
}