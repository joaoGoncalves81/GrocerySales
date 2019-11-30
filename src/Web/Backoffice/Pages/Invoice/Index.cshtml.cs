using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Backoffice.Pages.Invoice
{
    public class IndexModel : PageModel
    {
        private readonly GroceryContext _context;

        public IndexModel(GroceryContext context)
        {
            _context = context;
        }
        public IList<InvoiceConfig> Configs { get; set; }
        public async Task OnGetAsync()
        {
            Configs = await _context.invoiceConfigs.ToListAsync();
        }

        public IActionResult OnPost(InvoiceConfig invoiceConfig)
        {
            if(ModelState.IsValid)
            {
                //TODO Save Config
                return RedirectToPage();
            }
            return Page();
        }
    }
}
