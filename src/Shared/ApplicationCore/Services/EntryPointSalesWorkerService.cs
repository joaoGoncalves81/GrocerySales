﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.DTOs;
using ApplicationCore.Entities;
using ApplicationCore.Entities.BasketAggregate;
using ApplicationCore.Entities.OrderAggregate;
using ApplicationCore.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApplicationCore.Services
{
    public class EntryPointSalesWorkerService : IEntryPointSalesWorkerService
    {
        private const string WORKER_USER = "worker@saborcomtradicao.com";
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EntryPointSalesWorkerService> _logger;
        private readonly EmailSettings _settings;
        private static Random _rnd = new Random();

        public EntryPointSalesWorkerService(
            IServiceProvider serviceScope,
            ILogger<EntryPointSalesWorkerService> logger,
            IOptions<EmailSettings> options)
        {
            _serviceProvider = serviceScope;
            _logger = logger;
            _settings = options.Value;
        }
        public async Task ExecuteAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var invoiceRepository =
                    scope.ServiceProvider
                        .GetService<IAsyncRepository<InvoiceConfig>>();

                var catalogRepository =
                    scope.ServiceProvider
                        .GetService<IAsyncRepository<CatalogItem>>();

                var basketRepository =
                    scope.ServiceProvider
                        .GetService<IAsyncRepository<Basket>>();

                var orderService =
                    scope.ServiceProvider
                        .GetService<IOrderService>();

                var invoiceService =
                    scope.ServiceProvider
                        .GetService<IInvoiceService>();

                var emailSender =
                    scope.ServiceProvider
                        .GetService<IEmailSender>();

                var invoiceConfigs = await invoiceRepository.ListAllAsync();

                if (invoiceConfigs.Count == 0)
                {
                    _logger.LogWarning("No Invoice Configs found!");
                    return;
                }

                var dayConfig = invoiceConfigs.SingleOrDefault(x => x.Week == DateTime.Now.DayOfWeek && x.IsActive);
                if (dayConfig == null)
                {
                    _logger.LogWarning("Day {week} is not ative", DateTime.Now.DayOfWeek);
                    return;
                }

                var maxTotal = dayConfig.NrInvoices * dayConfig.MaxValue;
                //Get total products
                var spec = new CatalogFilterSpecification(true);
                var catalog = await catalogRepository.ListAsync(spec);
                List<Order> orders = new List<Order>();
                //Check if already send today
                var workerOrders = await orderService.GetOrdersAsync(WORKER_USER);
                var ordersOfToday = workerOrders.Where(x => x.OrderDate.Date == DateTime.Now.Date).ToList();
                if(ordersOfToday.Count > 0)
                {
                    _logger.LogWarning("There already orders for today, ignoring...");
                    return;
                }
                for (int i = 0; i < dayConfig.NrInvoices; i++)
                {
                    await DeleteWorkerBasket(basketRepository);

                    var basket = new Basket() { BuyerId = WORKER_USER, CreatedDate = DateTime.Now };
                    await basketRepository.AddAsync(basket);

                    decimal amountAvailable = dayConfig.MaxValue;
                    while(basket.Total() < dayConfig.MaxValue)
                    {
                        //Get available Products

                        var availableProducts = catalog.Where(x => 
                                (x.Price.HasValue && x.Price.Value <= amountAvailable) ||
                                (!x.Price.HasValue && x.CatalogType.Price <= amountAvailable))
                            .ToList();
                        if(availableProducts.Count == 0)
                        {
                            _logger.LogDebug("No more products to add to basket");
                            break;
                        }
                        _logger.LogDebug("{count} available products", availableProducts.Count);
                        
                        //Pick one product
                        var r = _rnd.Next(availableProducts.Count);
                        var product = availableProducts[r];
                        var priceUnit = product.Price ?? product.CatalogType.Price;
                        _logger.LogDebug("Pick product {name} for {price}", product.Name, priceUnit);
                        //Add to basket
                        basket.AddItem(product.Id, priceUnit, addToExistingItem: true);
                        amountAvailable -= priceUnit;
                        _logger.LogDebug("Available amount: {amount}", amountAvailable);
                    }

                    //Save basket
                    await basketRepository.UpdateAsync(basket);

                    //Total
                    _logger.LogInformation("Created Basket for Invoice {nr} with total: {total}", i + 1, basket.Total());

                    //Create Order
                    try
                    {
                        var address = new Address(string.Empty, "Mercado de Loulé - Banca nº 44, Praça da Republica", "Loulé", "Portugal", "8100-270");
                        var resOrder = await orderService.CreateOrderAsync(basket.Id, string.Empty, null, null, null, false, 0, null, true, PaymentType.CASH);
                        orders.Add(resOrder);
                        _logger.LogInformation("Successuful creating order {num} with total {total}", resOrder.Id, resOrder.Total());
                    }
                    catch (RegisterInvoiceException ex)
                    {
                        _logger.LogError(ex, "Erro ao gerar fatura na sage");
                    }
                    //Delete Basket
                    await basketRepository.DeleteAsync(basket);
                }
                //Send Emails
                if (orders.Count > 0)
                {
                    //Send Email to client (from: info.saborcomtradicao@gmail.com)
                    var body = "Olá, foram geradas as faturas em anexo para este dia! <br>" +
                        "Podes consultar as faturas no backoffice do sabor com tradição: <br>" +
                        "http://backoffice.saborcomtradicao.com/sales <br>" +
                        "Ou no portal da sage: <br>" +
                        "https://app.pt.sageone.com/facturacao <br>" +
                        "<br><strong>Muito obrigado e até amanhã!</strong><br>" +
                        "João Gonçalves<br>" +
                        "<img src='https://vendas.saborcomtradicao.com//images/logo-sabor.png' alt='Sabor Com Tradição'/>";

                    List<(string, byte[])> files = new List<(string, byte[])>();
                    foreach (var order in orders)
                    {
                        var invoiceBytes = await invoiceService.GetPDFInvoiceAsync(SageApplicationType.SALESWEB, order.SalesInvoiceId.Value);
                        if (invoiceBytes != null)
                        {
                            
                            files.Add(($"FaturaSaborComTradicao#{order.Id}.pdf", invoiceBytes));
                        }
                        else
                        {
                            _logger.LogError("Erro: O registo de venda foi efectuado com sucesso mas não foi possível obter a fatura. Email não enviado!");
                        }
                    }
                    try
                    {
                        await emailSender.SendEmailAsync(
                        _settings.FromOrderEmail,
                        _settings.CCEmails,
                        $"Sabor com Tradição - Faturação do dia {DateTime.Now.ToString("yyyy-MM-dd")}",
                        body,
                        null,
                        null,
                        files);
                    }
                    catch (SendEmailException ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email");
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something got wrong in worker: ");
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var emailSender =
                    scope.ServiceProvider
                        .GetService<IEmailSender>();

                    await emailSender.SendEmailAsync(
                            _settings.FromInfoEmail,
                            _settings.CCEmails,
                            "Erro no Sales Worker",
                            ex.ToString());
                }
                catch(Exception ex2)
                {
                    _logger.LogError(ex2, "Error sending email");
                }
            }
        }

        private async Task DeleteWorkerBasket(IAsyncRepository<Basket> basketRepository)
        {
            var basket = await basketRepository.GetSingleBySpecAsync(new BasketWithItemsSpecification(WORKER_USER));
            if(basket != null)
            {
                await basketRepository.DeleteAsync(basket);
            }
        }
    }
}
