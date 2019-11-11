using ApplicationCore.Entities;
using System.Collections.Generic;

namespace ApplicationCore.Entities.OrderAggregate
{
    public class OrderItem : BaseEntity
    {
        public CatalogItemOrdered ItemOrdered { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int Units { get; private set; }
        public string CustomizeName { get; private set; }

        //public ICollection<OrderItemDetail> Details { get; set; } = new List<OrderItemDetail>();

        protected OrderItem()
        {
        }
        public OrderItem(CatalogItemOrdered itemOrdered, decimal unitPrice, int units, string customizeName = null)
        {
            ItemOrdered = itemOrdered;
            UnitPrice = unitPrice;
            Units = units;
            CustomizeName = customizeName;
        }

        public void UpdateItemPrice(decimal newPrice)
        {
            this.UnitPrice = newPrice;
        }
    }
}
