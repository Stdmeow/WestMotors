using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // Для IFormFile

namespace WestMotorsApp.Models
{
    public class Car
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Марка обязательна")]
        [StringLength(50)]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Модель обязательна")]
        [StringLength(50)]
        public string Model { get; set; }

        [Required(ErrorMessage = "Год выпуска обязателен")]
        [Range(1900, 2100, ErrorMessage = "Некорректный год выпуска")]
        public int ManufactureYear { get; set; }

        [Required(ErrorMessage = "Пробег обязателен")]
        [Range(0, 1000000, ErrorMessage = "Некорректный пробег")]
        public int Mileage { get; set; }

        [Required(ErrorMessage = "VIN-номер обязателен")]
        [StringLength(17, MinimumLength = 17, ErrorMessage = "VIN-номер должен содержать 17 символов")]
        public string VIN { get; set; }

        [Required(ErrorMessage = "Цена обязательна")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Состояние обязательно")]
        [StringLength(50)]
        public string Condition { get; set; } // Например: "Новый", "Б/У", "После ремонта"

        [Required(ErrorMessage = "Дата поступления обязательна")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime ArrivalDate { get; set; } = DateTime.Now; // По умолчанию текущая дата

        [StringLength(500)]
        public string? PhotoUrl { get; set; } // Путь к фотографии автомобиля

        [NotMapped] // Это свойство не будет отображаться в базе данных
        [Display(Name = "Загрузить фото")]
        public IFormFile? PhotoFile { get; set; } // Для загрузки файла через форму

        // Навигационные свойства
        public ICollection<Deal>? Deals { get; set; }
        public ICollection<AutoServiceEntry>? AutoServiceEntries { get; set; }
        public ICollection<ApplicationRequest>? ApplicationRequests { get; set; }
    }
}