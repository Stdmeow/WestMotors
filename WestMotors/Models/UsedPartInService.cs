using System.ComponentModel.DataAnnotations;

namespace WestMotorsApp.Models
{
    public class UsedPartInService
    {
        public int ServiceId { get; set; }
        public AutoServiceEntry AutoServiceEntry { get; set; }

        public int PartId { get; set; }
        public SparePart SparePart { get; set; }

        [Required(ErrorMessage = "Количество использованных запчастей обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество не может быть меньше 1")]
        [Display(Name = "Количество использовано")]
        public int QuantityUsed { get; set; }
    }
}