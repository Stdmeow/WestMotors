using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WestMotorsApp.Models
{
    public class Deal
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Автомобиль обязателен")]
        [Display(Name = "Автомобиль")]
        public int CarId { get; set; }
        public Car Car { get; set; }

        [Display(Name = "Продавец")]
        public string SellerId { get; set; }
        [ForeignKey("SellerId")]
        public ApplicationUser Seller { get; set; }

        [Required(ErrorMessage = "Покупатель обязателен")]
        [Display(Name = "Покупатель")]
        public int BuyerId { get; set; }
        public Client Buyer { get; set; }

        [Required(ErrorMessage = "Дата сделки обязательна")]
        [Display(Name = "Дата сделки")]
        [DataType(DataType.Date)]
        public DateTime DealDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Итоговая стоимость обязательна")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Итоговая стоимость")]
        public decimal FinalCost { get; set; }

        [Required(ErrorMessage = "Способ оплаты обязателен")]
        [StringLength(50)]
        [Display(Name = "Способ оплаты")]
        public string PaymentMethod { get; set; }
    }
}