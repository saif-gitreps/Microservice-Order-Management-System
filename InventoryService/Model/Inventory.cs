using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.Model
{
    [Table("Inventory")]
    public class Inventory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

       
        [Required]
        public int Quantity { get; set; }

        // Quantity reserved for pending orders.
        // When order is created, quantity is reserved.
        // When order is confirmed, reserved quantity is deducted from total quantity.
        // When order is cancelled, reserved quantity is released.

        [Required]
        public int ReservedQuantity { get; set; }

    
        // Available quantity = Total Quantity - Reserved Quantity.
        // This is the quantity that can be allocated to new orders.
    
        public int AvailableQuantity => Quantity - ReservedQuantity;
    }
}
