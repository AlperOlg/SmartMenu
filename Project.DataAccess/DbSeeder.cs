using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;

namespace Project.DataAccess
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(DbContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            // 1. DÜZENLEME: IdentityRole yerine projenin AppRole sınıfı kullanıldı
            string[] roles = { "Admin", "RestorantSahibi", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole { Name = role });
                }
            }

            // 2. DÜZENLEME: IdentityUser yerine AppUser kullanıldı ve FullName zorunlu alanı dolduruldu
            var adminEmail = "admin@test.com";
            AppUser adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Sistem Yöneticisi" // Eklendi
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var ownerEmail = "sahip@test.com";
            AppUser ownerUser = await userManager.FindByEmailAsync(ownerEmail);
            if (ownerUser == null)
            {
                ownerUser = new AppUser
                {
                    UserName = "restoransahibi",
                    Email = ownerEmail,
                    EmailConfirmed = true,
                    FullName = "Ahmet Şef" // Eklendi
                };
                var result = await userManager.CreateAsync(ownerUser, "Sahip123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(ownerUser, "RestorantSahibi");
                }
            }

            var customerEmail = "musteri@test.com";
            AppUser customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new AppUser
                {
                    UserName = "musteri",
                    Email = customerEmail,
                    EmailConfirmed = true,
                    FullName = "Mehmet Yılmaz" // Eklendi
                };
                var result = await userManager.CreateAsync(customerUser, "Musteri123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                }
            }

            // --- İŞ MANTIĞI TABLOLARI SEED İŞLEMİ ---

            if (!await context.Set<Restaurant>().AnyAsync())
            {
                var sampleRestaurant = new Restaurant
                {
                    Name = "Gusto Bella Restoran",
                    OwnerId = ownerUser.Id, // Artık bir int ID eşleşiyor
                    CreatedAt = DateTime.UtcNow
                };

                await context.Set<Restaurant>().AddAsync(sampleRestaurant);
                await context.SaveChangesAsync();

                var cat1 = new Category { Name = "Ana Yemekler", RestaurantId = sampleRestaurant.Id };
                var cat2 = new Category { Name = "İçecekler", RestaurantId = sampleRestaurant.Id };
                await context.Set<Category>().AddRangeAsync(cat1, cat2);
                await context.SaveChangesAsync();

                var item1 = new MenuItem { Name = "Acılı Adana Kebap", Description = "Zırh kıyması, özel baharatlar ve közlenmiş biber ile", Price = 340.00m, CategoryId = cat1.Id, RestaurantId = sampleRestaurant.Id };
                var item2 = new MenuItem { Name = "Kuzu Şiş", Description = "Közlenmiş domates ve pilav eşliğinde", Price = 380.00m, CategoryId = cat1.Id, RestaurantId = sampleRestaurant.Id };
                var item3 = new MenuItem { Name = "Ev Yapımı Yayık Ayranı", Description = "Bol köpüklü soğuk ayran", Price = 45.00m, CategoryId = cat2.Id, RestaurantId = sampleRestaurant.Id };
                await context.Set<MenuItem>().AddRangeAsync(item1, item2, item3);

                // 3. DÜZENLEME: TableNumber string tipe çevrildi ve modeldeki IsOccupied set edildi
                var table1 = new Table { TableNumber = "1", IsOccupied = false, RestaurantId = sampleRestaurant.Id };
                var table2 = new Table { TableNumber = "2", IsOccupied = true, RestaurantId = sampleRestaurant.Id };
                var table3 = new Table { TableNumber = "3", IsOccupied = false, RestaurantId = sampleRestaurant.Id };
                var table4 = new Table { TableNumber = "4", IsOccupied = false, RestaurantId = sampleRestaurant.Id };
                await context.Set<Table>().AddRangeAsync(table1, table2, table3, table4);

                await context.SaveChangesAsync();
            }
        }
    }
}