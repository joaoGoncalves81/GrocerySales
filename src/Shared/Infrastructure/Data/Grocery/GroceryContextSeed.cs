using ApplicationCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class GroceryContextSeed
    {
        private static List<DayOfWeek> _weeks = new List<DayOfWeek> { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
        public static async Task SeedAsync(GroceryContext context)
        {

            if (!context.CatalogItems.Any(x => x.Id == -1) && context.CatalogTypes.Any())
            {
                context.CatalogItems.Add(new CatalogItem
                {
                    Id = -1,
                    Name = "Produto Personalizado",
                    ShowOnShop = false,
                    CatalogType = context.CatalogTypes.First()
                });

                await context.SaveChangesAsync();
            }

            if(!context.invoiceConfigs.Any())
            {
                foreach (var item in _weeks)
                {
                    context.invoiceConfigs.Add(new InvoiceConfig
                    {
                        Week = item
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        public static void EnsureDatabaseMigrations(GroceryContext groceryContext)
        {
            groceryContext.Database.Migrate();
        }
    }
}
