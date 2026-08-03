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
            string[] roles = { "Admin", "Owner", "Customer", "Employee" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new AppRole { Name = roleName });
                }
            }
            var adminEmail = "admin@test.com";
            AppUser adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Sistem Yöneticisi"
                };
                await userManager.CreateAsync(adminUser, "Password!23");
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            var firstOwnerEmail = "owner.anadolu@test.com";
            AppUser firstOwnerUser = await userManager.FindByEmailAsync(firstOwnerEmail);
            if (firstOwnerUser == null)
            {
                firstOwnerUser = new AppUser
                {
                    UserName = "owner.anadolu",
                    Email = firstOwnerEmail,
                    EmailConfirmed = true,
                    FullName = "Ahmet Şef"
                };
                await userManager.CreateAsync(firstOwnerUser, "Password!23");
            }

            if (!await userManager.IsInRoleAsync(firstOwnerUser, "Owner"))
            {
                await userManager.AddToRoleAsync(firstOwnerUser, "Owner");
            }

            var secondOwnerEmail = "owner.ege@test.com";
            AppUser secondOwnerUser = await userManager.FindByEmailAsync(secondOwnerEmail);
            if (secondOwnerUser == null)
            {
                secondOwnerUser = new AppUser
                {
                    UserName = "owner.ege",
                    Email = secondOwnerEmail,
                    EmailConfirmed = true,
                    FullName = "Ayşe Şef"
                };
                await userManager.CreateAsync(secondOwnerUser, "Password!23");
            }

            if (!await userManager.IsInRoleAsync(secondOwnerUser, "Owner"))
            {
                await userManager.AddToRoleAsync(secondOwnerUser, "Owner");
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
                    FullName = "Mehmet Yılmaz"
                };
                await userManager.CreateAsync(customerUser, "Password!23");
            }

            if (!await userManager.IsInRoleAsync(customerUser, "Customer"))
            {
                await userManager.AddToRoleAsync(customerUser, "Customer");
            }

            var employeeEmail = "calisan.anadolu@test.com";
            AppUser employeeUser = await userManager.FindByEmailAsync(employeeEmail);
            if (employeeUser == null)
            {
                employeeUser = new AppUser
                {
                    UserName = "calisan.anadolu",
                    Email = employeeEmail,
                    EmailConfirmed = true,
                    FullName = "Zeynep Garson"
                };
                await userManager.CreateAsync(employeeUser, "Password!23");
            }

            if (!await userManager.IsInRoleAsync(employeeUser, "Employee"))
            {
                await userManager.AddToRoleAsync(employeeUser, "Employee");
            }

            // --- İŞ MANTIĞI TABLOLARI SEED İŞLEMİ ---

            if (!await context.Set<Restaurant>().AnyAsync())
            {
                var anadoluSofrasi = new Restaurant
                {
                    Name = "Anadolu Sofrası",
                    OwnerId = firstOwnerUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                var secondRestaurant = new Restaurant
                {
                    Name = "Ege Lezzetleri",
                    OwnerId = secondOwnerUser.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Set<Restaurant>().AddRangeAsync(anadoluSofrasi, secondRestaurant);
                await context.SaveChangesAsync();

                employeeUser.AccessRestaurantId = anadoluSofrasi.Id;
                employeeUser.AccessLevel = EmployeeAccessLevel.FullAccess;
                await userManager.UpdateAsync(employeeUser);

                var cat1 = new Category { Name = "Ana Yemekler", RestaurantId = anadoluSofrasi.Id };
                var cat2 = new Category { Name = "İçecekler", RestaurantId = anadoluSofrasi.Id };
                await context.Set<Category>().AddRangeAsync(cat1, cat2);
                await context.SaveChangesAsync();

                var item1 = new MenuItem { Name = "Acılı Adana Kebap", Description = "Zırh kıyması, özel baharatlar ve közlenmiş biber ile", Price = 340.00m, CategoryId = cat1.Id, RestaurantId = anadoluSofrasi.Id };
                var item2 = new MenuItem { Name = "Kuzu Şiş", Description = "Közlenmiş domates ve pilav eşliğinde", Price = 380.00m, CategoryId = cat1.Id, RestaurantId = anadoluSofrasi.Id };
                var item3 = new MenuItem { Name = "Ev Yapımı Yayık Ayranı", Description = "Bol köpüklü soğuk ayran", Price = 45.00m, CategoryId = cat2.Id, RestaurantId = anadoluSofrasi.Id };
                await context.Set<MenuItem>().AddRangeAsync(item1, item2, item3);

                var table1 = new Table { TableNumber = "1", IsOccupied = false, RestaurantId = anadoluSofrasi.Id };
                var table2 = new Table { TableNumber = "2", IsOccupied = true, RestaurantId = anadoluSofrasi.Id };
                var table3 = new Table { TableNumber = "3", IsOccupied = false, RestaurantId = anadoluSofrasi.Id };
                var table4 = new Table { TableNumber = "4", IsOccupied = false, RestaurantId = anadoluSofrasi.Id };
                await context.Set<Table>().AddRangeAsync(table1, table2, table3, table4);

                var order = new Order { AppUser = customerUser, AppUserId = customerUser.Id, Table = table2, TableId = table2.Id, Restaurant = anadoluSofrasi, RestaurantId = anadoluSofrasi.Id, IsPaid = false, PaidAt = null, OrderDate = DateTime.UtcNow, TotalAmount = 340m, DiscountAmount = 0, PointsEarned = 34, PointsSpent = 0 };
                var orderItem = new OrderItem { Order = order, OrderId = order.Id, MenuItem = item1, MenuItemId = item1.Id, Quantity = 1, UnitPrice = item1.Price };
                await context.Set<Order>().AddAsync(order);
                await context.Set<OrderItem>().AddAsync(orderItem);

                await context.SaveChangesAsync();
            }
        }
    }
}