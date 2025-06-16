using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace WestMotorsApp.Models
{
    public class AutoServiceEntry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Автомобиль обязателен")]
        [Display(Name = "Автомобиль")]
        public int CarId { get; set; }
        public Car Car { get; set; }

        [Required(ErrorMessage = "Тип работ обязателен")]
        [StringLength(100)]
        [Display(Name = "Тип работ")]
        public string WorkType { get; set; }

        [Required(ErrorMessage = "Сумма затрат на ремонт обязательна")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Сумма затрат")]
        public decimal RepairCost { get; set; }

        [Display(Name = "Механик")]
        public string? MechanicId { get; set; } // Id механика
        [ForeignKey("MechanicId")]
        public ApplicationUser? Mechanic { get; set; } // Навигационное свойство

        [Required(ErrorMessage = "Дата обслуживания обязательна")]
        [Display(Name = "Дата обслуживания")]
        [DataType(DataType.Date)]
        public DateTime ServiceDate { get; set; } = DateTime.Now;

        public ICollection<UsedPartInService>? UsedParts { get; set; }
    }
}