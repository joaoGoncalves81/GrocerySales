﻿using ApplicationCore.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Identity
{
    public static class AppIdentityDbContextSeed
    {
        private static string _roleAdmin = "Admin";
        private static string _roleGroceryAdmin = "GroceryAdmin";
        private static string _roleStaff = "Staff";

        public static void EnsureDatabaseMigrations(AppIdentityDbContext context)
        {            
            context.Database.Migrate();
        }

        public static async Task EnsureRoleAdminCreated(IServiceProvider services)
        {

            var rolemanager = services.GetRequiredService<RoleManager<IdentityRole>>();

            if (rolemanager.FindByNameAsync(_roleAdmin).Result == null)
            {
                var result = await rolemanager.CreateAsync(new IdentityRole(_roleAdmin));
            }

            if (rolemanager.FindByNameAsync(_roleStaff).Result == null)
            {
                var result = await rolemanager.CreateAsync(new IdentityRole(_roleStaff));
            }
            if (rolemanager.FindByNameAsync(_roleGroceryAdmin).Result == null)
            {
                var result = await rolemanager.CreateAsync(new IdentityRole(_roleGroceryAdmin));
            }


        }

        public static void SeedAsync(UserManager<ApplicationUser> userManager, AppIdentityDbContext context = null)
        {            
        }
    }
}
