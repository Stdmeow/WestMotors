using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WestMotorsApp.Models
{
    public class SparePart
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название запчасти обязательно")]
        [StringLength(100)]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Применимость к модели")]
        public string? CarModelApplicability { get; set; }

        [Required(ErrorMessage = "Количество в наличии обязательно")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        [Display(Name = "Количество в наличии")]
        public int QuantityInStock { get; set; }

        public ICollection<UsedPartInService>? UsedInServices { get; set; }
    }
}