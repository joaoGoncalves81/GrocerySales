using ApplicationCore.Entities;
using ApplicationCore.Entities.OrderAggregate;
using ApplicationCore.Interfaces;
using ApplicationCore.Specifications;
using Ardalis.GuardClauses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.DTOs;
using System;
using ApplicationCore.Exceptions;
using ApplicationCore.Entities.BasketAggregate;

namespace ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IAsyncRepository<CustomizeOrder> _customizeOrderRepository;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;
        private readonly IAsyncRepository<CatalogType> _catalogRepository;
        private readonly IInvoiceService _invoiceService;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IAsyncRepository<CustomizeOrder> customizeOrderRepository,
            IAsyncRepository<CatalogType> catalogRepository,
            IInvoiceService invoiceService)
        {
            _orderRepository = orderRepository;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
            _customizeOrderRepository = customizeOrderRepository;
            _catalogRepository = catalogRepository;
            _invoiceService = invoiceService;
        }

        public async Task<Order> CreateOrderAsync(int basketId, string phoneNumber, int? taxNumber, Address shippingAddress, Address billingAddress, bool useBillingSameAsShipping, decimal shippingCost, string customerEmail = null, bool registerInvoice = false, PaymentType paymentType = PaymentType.CASH)
        {
            //TODO: check price
            var basket = await _basketRepository.GetByIdAsync(basketId);
            Guard.Against.NullBasket(basketId, basket);
            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                if (!item.CatalogTypeId.HasValue)
                {
                    var catalogItem = await _itemRepository.GetByIdAsync(item.CatalogItemId);
                    var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, catalogItem.PictureUri);
                    var orderItem = new OrderItem(itemOrdered, item.UnitPrice, item.Quantity, item.CustomizeName);
                    items.Add(orderItem);
                }
                else
                {
                    var catalogType = await _catalogRepository.GetByIdAsync(item.CatalogTypeId.Value);
                    var itemOrdered = new CatalogItemOrdered(0, null, null);
                    var customizeItem = new CustomizeItemOrdered(
                        item.CatalogTypeId.Value,
                        item.CustomizeDescription,
                        item.CustomizeName,
                        item.CustomizeColors,
                        catalogType.Name,
                        catalogType.PictureUri);

                    var orderItem = new OrderItem(itemOrdered, 0, item.Quantity);

                    items.Add(orderItem);
                }
            }
            var order = new Order(basket.BuyerId, phoneNumber, taxNumber, shippingAddress, billingAddress, useBillingSameAsShipping, items, shippingCost, basket.Observations, customerEmail);
            var savedOrder = await _orderRepository.AddAsync(order);

            if (registerInvoice)
            {
                savedOrder.OrderState = OrderStateType.SUBMITTED;
                SageResponseDTO response;
                //From sales rename product name

                foreach (var item in savedOrder.OrderItems.Where(x => x.ItemOrdered.CatalogItemId == -1).ToList())
                {
                    item.ItemOrdered.Rename(item.CustomizeName);
                }

                try
                {
                    response = await _invoiceService.RegisterInvoiceAsync(SageApplicationType.SALESWEB, savedOrder);
                }
                catch (Exception ex)
                {
                    throw new RegisterInvoiceException(ex.ToString());
                }

                if (response.InvoiceId.HasValue)
                {
                    savedOrder.SalesInvoiceId = response.InvoiceId.Value;
                    savedOrder.SalesInvoiceNumber = response.InvoiceNumber;

                    await _orderRepository.UpdateAsync(savedOrder);
                    //Generate Payment
                    try
                    {
                        var responsePayment = await _invoiceService.RegisterPaymentAsync(SageApplicationType.SALESWEB, savedOrder.SalesInvoiceId.Value, savedOrder.Total(), paymentType);
                        if (responsePayment.PaymentId.HasValue)
                        {
                            savedOrder.SalesPaymentId = responsePayment.PaymentId.Value;
                            await _orderRepository.UpdateAsync(savedOrder);
                        }
                        else
                            throw new RegisterInvoiceException($"Fatura gerada com sucesso mas com erro de pagamento: {responsePayment?.Message}");
                    }
                    catch (Exception ex)
                    {
                        throw new RegisterInvoiceException($"Fatura gerada com sucesso mas com erro de pagamento: {ex}");
                    }
                }
                else //Something wrong
                {
                    throw new RegisterInvoiceException(response.Message);
                }
            }

            return savedOrder;
        }


        public async Task UpdateOrderState(int id, OrderStateType orderState, bool isCustomizeOrder = false)
        {
            if (!isCustomizeOrder)
            {
                var order = await _orderRepository.GetByIdAsync(id);
                if (order != null)
                {
                    order.OrderState = orderState;
                    await _orderRepository.UpdateAsync(order);
                }
            }
            else
            {
                var order = await _customizeOrderRepository.GetByIdAsync(id);
                if (order != null)
                {
                    order.OrderState = orderState;
                    await _customizeOrderRepository.UpdateAsync(order);
                }
            }
        }
        public async Task<Order> GetOrderAsync(int id)
        {
            var spec = new OrdersSpecification(id);
            return (await _orderRepository.ListAsync(spec)).FirstOrDefault();
        }

        public async Task<List<Order>> GetOrdersAsync(string buyerId)
        {
            var spec = new OrdersSpecification(buyerId);
            return await _orderRepository.ListAsync(spec);
        }
       
        public async Task UpdateOrderInvoiceAsync(int id, long? invoiceId, string invoiceNumber)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order != null)
            {
                order.SalesInvoiceId = invoiceId;
                order.SalesInvoiceNumber = invoiceNumber;
                await _orderRepository.UpdateAsync(order);
            }
        }
        public async Task UpdateOrderBillingAsync(int id, int? taxNumber, string customerEmail, Address billingAddress)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order != null)
            {
                order.UpdateBillingInfo(taxNumber, customerEmail, billingAddress);

                await _orderRepository.UpdateAsync(order);
            }
        }

        public async Task UpdateOrderItemsPrice(int orderId, List<Tuple<int, decimal>> items)
        {
            var order = await GetOrderAsync(orderId);
            if(order != null && items?.Count > 0)
            {
                foreach (var item in items)
                {
                    var orderItem = order.OrderItems.SingleOrDefault(x => x.Id == item.Item1);
                    if (orderItem != null)
                        orderItem.UpdateItemPrice(item.Item2);
                }
                await _orderRepository.UpdateAsync(order);
            }
        }

    }
}
