﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SalesWeb.Interfaces;
using SalesWeb.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using ApplicationCore;

namespace SalesWeb.Pages.Product
{
    public class IndexModel : PageModel
    {
        private readonly ICatalogService _catalogService;
        private readonly IAppLogger<IndexModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _settings;

        public IndexModel(ICatalogService catalogService, 
            IAppLogger<IndexModel> logger,
            IEmailSender emailSender,
            IOptions<EmailSettings> options)
        {
            _catalogService = catalogService;
            _logger = logger;
            _emailSender = emailSender;
            _settings = options.Value;
        }
        public ProductViewModel ProductModel { get; set; } = new ProductViewModel();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();
            ProductModel = await _catalogService.GetCatalogItem(id);

            if (ProductModel == null)
                return NotFound();

            ViewData["ProductReferences"] = new SelectList(ProductModel.ProductReferences, "Sku", "Name");

            //Update default price
            //decimal attrDefaultPrice = 0M;
            //foreach (var item in ProductModel.Attributes.GroupBy(x => x.AttributeType))
            //{                
            //    attrDefaultPrice += item.First().Attributes.First().Price ?? 0;
            //}
            //ProductModel.ProductTotalPrice = ProductModel.ProductPrice + attrDefaultPrice;
            return Page();
        }

        public async Task<IActionResult> OnPostSendMessageAsync(string ContactEmailAddress, string Message, string ProductSKU)
        {
            await _emailSender.SendGenericEmailAsync(
                _settings.FromInfoEmail,
                _settings.CCEmails,
                $"Pedido de dúvida do Produto {ProductSKU}",
                $"De: {ContactEmailAddress}<br>" +
                $"Mensagem: {Message}");

            StatusMessage = "A sua mensagem foi enviada com sucesso";
            return RedirectToPage(new { id = ProductSKU });
        }
    }
}