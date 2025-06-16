using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WestMotorsApp.Models
{
    public class ApplicationRequest
    {
        public int Id { get; set; }

        // Изменено: ClientId сделан nullable (int?), т.к. не каждый пользователь обязательно является клиентом
        [Display(Name = "Клиент")]
        public int? ClientId { get; set; }
        public Client? Client { get; set; } // Также nullable навигационное свойство

        [Required(ErrorMessage = "Автомобиль обязателен")]
        [Display(Name = "Автомобиль")]
        public int CarId { get; set; }
        public Car Car { get; set; } = default!; // Инициализация, чтобы избежать NullReferenceException

        [Required(ErrorMessage = "Тип заявки обязателен")]
        [StringLength(100)] // Увеличил длину
        [Display(Name = "Тип заявки")]
        public string RequestType { get; set; } = string.Empty; // Инициализация

        [Required(ErrorMessage = "Дата заявки обязательна")]
        [Display(Name = "Дата заявки")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow; // Использовать UtcNow по умолчанию

        [Required(ErrorMessage = "Статус заявки обязателен")]
        [StringLength(50)]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Новая"; // Статус по умолчанию

        // Дополнительные поля для связи с пользователем и его данными
        [StringLength(256)]
        [Display(Name = "Email пользователя")]
        public string? UserEmail { get; set; } // Может быть null, если заявка от анонима или менеджера

        [Required(ErrorMessage = "ФИО клиента обязательно")]
        [StringLength(100)]
        [Display(Name = "ФИО клиента")]
        public string ClientFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Контактные данные обязательны")]
        [StringLength(200)]
        [Display(Name = "Контактная информация")]
        public string ContactInfo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Способ связи обязателен")]
        [StringLength(50)]
        [Display(Name = "Предпочитаемый способ связи")]
        public string PreferredContactMethod { get; set; } = string.Empty;

        [StringLength(500)] // Добавляем ограничение длины
        public string? ManagerNotes { get; set; } // Заметки менеджера (могут быть null)
    }
}