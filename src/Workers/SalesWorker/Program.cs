using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Logging;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SalesWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AppIdentityDbContext>(options =>
                        options.UseMySql(hostContext.Configuration.GetConnectionString("IdentityConnection")));
                    services.AddDbContext<GroceryContext>(options =>
                        options.UseMySql(hostContext.Configuration.GetConnectionString("GroceryConnection")));

                    services.AddScoped(typeof(IAsyncRepository<>), typeof(EfGroceryRepository<>));
                    services.AddScoped<IOrderService, OrderService>();
                    services.AddScoped<IInvoiceService, InvoiceService>();
                    services.AddScoped<ISageService, SageService>();
                    services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
                    services.AddScoped<IAuthConfigRepository, AuthConfigRepository>();
                    services.AddTransient<IEmailSender, EmailSender>();
                    services.AddSingleton<IEntryPointSalesWorkerService, EntryPointSalesWorkerService>();

                    services.AddMemoryCache();

                    services.Configure<SageSettings>(hostContext.Configuration.GetSection("Sage"));
                    services.Configure<EmailSettings>(hostContext.Configuration.GetSection("Email"));

                    services.AddHostedService<Worker>();
                });
    }
}
