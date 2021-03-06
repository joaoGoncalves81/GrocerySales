﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ApplicationCore.Interfaces;
using System.Linq;
using ApplicationCore.Entities.OrderAggregate;
using SalesWeb.Extensions;
using ApplicationCore.Specifications;
using Microsoft.AspNetCore.Mvc;
using SalesWeb.ViewModels;
using ApplicationCore.Entities;

namespace SalesWeb.Pages.Order
{
    public partial class DetailModel : PageModel
    {
        private readonly IAsyncRepository<ApplicationCore.Entities.OrderAggregate.Order> _repository;
        private readonly IAsyncRepository<Country> _countryRepository;

        public DetailModel(IAsyncRepository<ApplicationCore.Entities.OrderAggregate.Order> repository,
            IAsyncRepository<Country> countryRepository)
        {
            _repository = repository;
            _countryRepository = countryRepository;
        }

        [BindProperty]
        public OrderViewModel OrderDetails { get; set; } = new OrderViewModel();

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var order = await _repository.GetSingleBySpecAsync(new GroceryOrdersSpecification(orderId));
            if (order == null)
                return NotFound();

            string countryName = "Portugal";
            if (int.TryParse(order.BillingToAddress?.Country, out int countryCode))
            {
                if (countryCode != 0)
                {
                    var country = await _countryRepository.GetByIdAsync(countryCode);
                    if (country != null)
                        countryName = country.Name;
                }
            }
            
            OrderDetails = new OrderViewModel()
            {
                OrderDate = order.OrderDate,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel()
                {
                    Discount = 0,
                    PictureUrl = oi.ItemOrdered.PictureUri,
                    ProductId = oi.ItemOrdered.CatalogItemId,
                    ProductName = oi.ItemOrdered.ProductName,
                    UnitPrice = oi.UnitPrice,
                    Units = oi.Units
                    //Attributes = _orderService.GetOrderAttributes(oi.ItemOrdered.CatalogItemId, oi.CatalogAttribute1, oi.CatalogAttribute2, oi.CatalogAttribute3)
                    //.Select(x => new OrderItemDetailViewModel
                    //{
                    //    Type = x.Type,
                    //    AttributeName = x.Name
                    //})
                    //.ToList()
                }).ToList(),

                OrderNumber = order.Id,
                InvoiceNr = order.SalesInvoiceNumber,
                InvoiceId = order.SalesInvoiceId.Value,
                BillingAddress = order.BillingToAddress,
                CountryName = countryName,
                Status = EnumHelper<OrderStateType>.GetDisplayValue(order.OrderState),
                CustomerEmail = order.CustomerEmail,
                Total = order.Total()
            };

            return Page();
        }
    }    
}
