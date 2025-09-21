using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        string[] roles = ["Admin", "Operator", "DepartmentManager"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed a default admin user (dev only)
        var adminEmail = "admin@lostandfound.local";
        var admin = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Rendszer Admin",
                Occupation = "Admin"
            };
            var createResult = await userManager.CreateAsync(admin, "Admin123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
        else
        {
            // Ensure Admin role and reset password to known dev password to recover login
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            admin.EmailConfirmed = true;
            await userManager.UpdateAsync(admin);
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
            await userManager.ResetPasswordAsync(admin, resetToken, "Admin123!");
        }

        // Seed default currencies if not present
        var db = userManager as IUserStore<ApplicationUser> as dynamic; // hack to reach DbContext via known DI in Program.cs
        try
        {
            var context = (db?.Context) as Microsoft.EntityFrameworkCore.DbContext;
            if (context != null)
            {
                var currencies = context.Set<Currency>();
                var denoms = context.Set<CurrencyDenomination>();
                if (!await currencies.AnyAsync())
                {
                    var huf = new Currency { Code = "HUF", Name = "Magyar forint", IsActive = true, SortOrder = 1 };
                    var eur = new Currency { Code = "EUR", Name = "Euro", IsActive = true, SortOrder = 2 };
                    await currencies.AddRangeAsync(huf, eur);
                    await context.SaveChangesAsync();

                    // HUF denominations (Ft)
                    var hufDenoms = new[]
                    {
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 20000, Label = "20000 Ft", SortOrder = 1, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 10000, Label = "10000 Ft", SortOrder = 2, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 5000, Label = "5000 Ft", SortOrder = 3, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 2000, Label = "2000 Ft", SortOrder = 4, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 1000, Label = "1000 Ft", SortOrder = 5, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 500, Label = "500 Ft", SortOrder = 6, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 200, Label = "200 Ft", SortOrder = 7, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 100, Label = "100 Ft", SortOrder = 8, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 50, Label = "50 Ft", SortOrder = 9, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 20, Label = "20 Ft", SortOrder = 10, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 10, Label = "10 Ft", SortOrder = 11, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = huf.Id, ValueMinor = 5, Label = "5 Ft", SortOrder = 12, IsActive = true },
                    };

                    // EUR denominations (€) in cents for minor units
                    var eurDenoms = new[]
                    {
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 5000, Label = "50 €", SortOrder = 1, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 2000, Label = "20 €", SortOrder = 2, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 1000, Label = "10 €", SortOrder = 3, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 500, Label = "5 €", SortOrder = 4, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 200, Label = "2 €", SortOrder = 5, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 100, Label = "1 €", SortOrder = 6, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 50, Label = "50 c", SortOrder = 7, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 20, Label = "20 c", SortOrder = 8, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 10, Label = "10 c", SortOrder = 9, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 5, Label = "5 c", SortOrder = 10, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 2, Label = "2 c", SortOrder = 11, IsActive = true },
                        new CurrencyDenomination{ CurrencyId = eur.Id, ValueMinor = 1, Label = "1 c", SortOrder = 12, IsActive = true },
                    };

                    await denoms.AddRangeAsync(hufDenoms);
                    await denoms.AddRangeAsync(eurDenoms);
                    await context.SaveChangesAsync();
                }
            }
        }
        catch { /* ignore */ }
    }
}
