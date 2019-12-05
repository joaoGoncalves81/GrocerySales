using System;
using System.Globalization;
using ApplicationCore;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using AutoMapper;
using Infrastructure.Data;
using Infrastructure.Identity;
using Infrastructure.Logging;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesWeb.Interfaces;
using SalesWeb.Services;

namespace SalesWeb
{
    public class Startup
    {
        private IServiceCollection _services;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            // use in-memory database
            //ConfigureTestingServices(services);

            // use real database            
            services.AddDbContext<GroceryContext>(options => 
                options.UseMySql(Configuration.GetConnectionString("GroceryConnection")));

            // Add Identity DbContext
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("IdentityConnection")));

            ConfigureServices(services);
            services.AddScoped<IInvoiceService, InvoiceTestService>();
        }

        public void ConfigureProductionServices(IServiceCollection services)
        {
            // use real database            
            services.AddDbContext<GroceryContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("GroceryConnection")));

            // Add Identity DbContext
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("IdentityConnection")));

            ConfigureServices(services);

            services.AddScoped<IInvoiceService, InvoiceService>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;                   
                    options.Password.RequireUppercase = false;
                })
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();
            

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireGroceryRole", policy => policy.RequireRole("GroceryAdmin"));
                options.AddPolicy("RequireGroceryStaff", policy => policy.RequireRole("Staff"));
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "salesApp";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Signout";                
                options.Cookie.IsEssential = true;
            });
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;               
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });

            // The Tempdata provider cookie is not essential. Make it essential
            // so Tempdata is functional when tracking is disabled.
            services.Configure<CookieTempDataProviderOptions>(options => {
                options.Cookie.IsEssential = true;
            });

            services.AddAutoMapper(typeof(Startup));

            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfGroceryRepository<>));

            services.AddScoped<ICatalogService, CachedCatalogService>();
            services.AddScoped<IBasketService, BasketService>();
            services.AddScoped<IBasketViewModelService, BasketViewModelService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IBasketRepository, BasketGroceryRepository>();
            services.AddScoped<CatalogService>();
            services.AddScoped<IShopService,ShopService>();
            services.Configure<AppSettings>(Configuration);
            services.Configure<SageSettings>(Configuration.GetSection("Sage"));
            services.Configure<EmailSettings>(Configuration.GetSection("Email"));
            services.AddSingleton<IUriComposer>(new UriComposer(Configuration.Get<CatalogSettings>()));           
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));           
            services.AddScoped<IAuthConfigRepository, AuthConfigRepository>();            
            services.AddScoped<ISageService, SageService>();
            services.AddTransient<IEmailSender, EmailSender>();
          
            // Add memory cache services
            services.AddMemoryCache();

            services.AddRazorPages(options =>
                {
                    options.Conventions.AuthorizeFolder("/", "RequireGroceryStaff");

                    options.Conventions.AddPageRoute("/Category/Index", "{id}/");
                    options.Conventions.AddPageRoute("/Category/Type/Index", "{cat}/{type}");
                    options.Conventions.AddPageRoute("/Product/Index", "produto/{id}");
                    options.Conventions.AddPageRoute("/Tag/Index", "tag/{tagName}");
                    options.Conventions.AddPageRoute("/Search/Index", "procurar/{q?}");
                    options.Conventions.AddPageRoute("/Customize/Index", "personalizar/");
                    options.Conventions.AddPageRoute("/Customize/Step2", "personalizar/passo2");
                    options.Conventions.AddPageRoute("/Customize/Step3", "personalizar/passo3");
                    options.Conventions.AddPageRoute("/Customize/Result", "personalizar/resultado");
                    options.Conventions.AddPageRoute("/Privacy", "privacidade");
                    options.Conventions.AddPageRoute("/NotFound", "pagina-nao-encontrada");
                    options.Conventions.AddPageRoute("/Error", "erro");
                    options.Conventions.AddPageRoute("/Account/Login", "conta/entrar");
                    options.Conventions.AddPageRoute("/Account/ConfirmEmail", "conta/confirmar-email");
                    options.Conventions.AddPageRoute("/Account/ForgotPassword", "conta/esqueceu-password");
                    options.Conventions.AddPageRoute("/Account/ForgotPasswordConfirmation", "conta/confirmacao-password");
                    options.Conventions.AddPageRoute("/Account/ResetPassword", "conta/recuperar-password");
                    options.Conventions.AddPageRoute("/Account/ResetPasswordConfirmation", "conta/confirmacao-recuperacao-password");
                    options.Conventions.AddPageRoute("/Account/Manage/Index", "conta/perfil");
                });

            _services = services;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            //app.UsePathBase("/loja");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                //app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("pt-PT"),
                // Formatting numbers, dates, etc.
                SupportedCultures = new[] { new CultureInfo("pt-PT") },
                // UI strings that we have localized.
                SupportedUICultures = new[] { new CultureInfo("pt-PT") }
            });

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (ctx, next) =>
            {
                await next();

                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
                {
                    //Re-execute the request so the user gets the error page
                    string originalPath = ctx.Request.Path.Value;
                    ctx.Items["originalPath"] = originalPath;
                    ctx.Request.Path = "/NotFound";
                    await next();
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
