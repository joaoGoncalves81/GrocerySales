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
        [BindProperty]
        public IList<InvoiceConfig> Configs { get; set; }

        public decimal MonthlyEstimate { get; set; }
        public async Task OnGetAsync()
        {
            Configs = await _context.invoiceConfigs.ToListAsync();
            
            MonthlyEstimate = CalculateMonthlyEstimate();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(ModelState.IsValid)
            {
                //TODO Save Config
                foreach (var item in Configs)
                {
                    _context.Entry(item).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
                return RedirectToPage();
            }
            return Page();
        }

        private decimal CalculateMonthlyEstimate()
        {
            if (Configs.Count == 0)
                return 0;
            var activesConfigs = Configs.Where(x => x.IsActive)
                .ToList();
            if (activesConfigs.Count == 0)
                return 0;
            var weekly = activesConfigs.Sum(x => x.NrInvoices * x.MaxValue);

            return weekly * 4; //4 weeks per month
        }
    }
}
