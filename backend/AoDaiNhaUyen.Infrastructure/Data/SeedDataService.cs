using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Domain.SeedData;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Data;

public sealed class SeedDataService(AppDbContext dbContext) : ISeedDataService
{
  public async Task SeedAllAsync()
  {
    await dbContext.Database.MigrateAsync();

    await SeedRolesAsync();
    await SeedCustomersAsync();
    await SeedCategoriesAsync();
    await SeedProductsAsync();
    await RemoveStaleCategoriesAsync();
  }

  private async Task SeedRolesAsync()
  {
    foreach (var roleName in DefaultRoles.Items)
    {
      var exists = await dbContext.Roles.AnyAsync(r => r.Name == roleName);
      if (!exists)
      {
        dbContext.Roles.Add(new Role { Name = roleName });
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedCategoriesAsync()
  {
    var parents = DefaultCategories.Items.Where(x => x.ParentSlug is null).ToList();
    foreach (var item in parents)
    {
      var existing = await dbContext.Categories.FirstOrDefaultAsync(x => x.Slug == item.Slug);
      if (existing is null)
      {
        dbContext.Categories.Add(new Category
        {
          Name = item.Name,
          Slug = item.Slug,
          Description = null,
          Parent = null,
          SortOrder = item.SortOrder,
          IsActive = true
        });
        continue;
      }

      existing.Name = item.Name;
      existing.Parent = null;
      existing.SortOrder = item.SortOrder;
      existing.IsActive = true;
    }

    await dbContext.SaveChangesAsync();

    var children = DefaultCategories.Items.Where(x => x.ParentSlug is not null).ToList();
    foreach (var item in children)
    {
      var parent = await dbContext.Categories.FirstAsync(c => c.Slug == item.ParentSlug);
      var existing = await dbContext.Categories.FirstOrDefaultAsync(x => x.Slug == item.Slug);

      if (existing is null)
      {
        dbContext.Categories.Add(new Category
        {
          Name = item.Name,
          Slug = item.Slug,
          Description = null,
          Parent = parent.Id,
          SortOrder = item.SortOrder,
          IsActive = true
        });
        continue;
      }

      existing.Name = item.Name;
      existing.Parent = parent.Id;
      existing.SortOrder = item.SortOrder;
      existing.IsActive = true;
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedCustomersAsync()
  {
    var customerRole = await dbContext.Roles.FirstAsync(x => x.Name == "customer");

    foreach (var item in DefaultCustomers.Items)
    {
      var user = await dbContext.Users
        .Include(x => x.UserRoles)
        .FirstOrDefaultAsync(x => x.Email == item.Email);

      if (user is null)
      {
        user = new User
        {
          FullName = item.FullName,
          Email = item.Email,
          Phone = item.Phone,
          PasswordHash = item.PasswordHash,
          Gender = item.Gender,
          Status = "active",
          EmailVerifiedAt = DateTime.UtcNow,
          PhoneVerifiedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
      }
      else
      {
        user.FullName = item.FullName;
        user.Phone = item.Phone;
        user.PasswordHash = item.PasswordHash;
        user.Gender = item.Gender;
        user.Status = "active";
        user.EmailVerifiedAt ??= DateTime.UtcNow;
        user.PhoneVerifiedAt ??= DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
      }

      if (!user.UserRoles.Any(x => x.RoleId == customerRole.Id))
      {
        user.UserRoles.Add(new UserRole { RoleId = customerRole.Id });
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task SeedProductsAsync()
  {
    await RemoveStaleProductsAsync();

    var materialsBySlug = DefaultMaterials.Items.ToDictionary(x => x.Slug, x => x.Name);

    foreach (var item in DefaultProducts.Items)
    {
      var category = await dbContext.Categories.FirstAsync(x => x.Slug == item.CategorySlug);
      var material = item.MaterialSlug is not null && materialsBySlug.TryGetValue(item.MaterialSlug, out var materialName)
        ? materialName
        : null;

      var productType = await ResolveProductTypeAsync(category);
      var product = await dbContext.Products
        .Include(x => x.Variants)
        .Include(x => x.Images)
        .FirstOrDefaultAsync(x => x.Slug == item.Slug);

      if (product is null)
      {
        product = new Product
        {
          CategoryId = category.Id,
          Name = item.Name,
          Slug = item.Slug,
          ProductType = productType,
          ShortDescription = item.ShortDescription,
          Description = item.LongDescription,
          Material = material,
          Brand = "Nha Uyen",
          Origin = "Viet Nam",
          Status = "active",
          IsFeatured = item.IsFeatured
        };

        dbContext.Products.Add(product);
      }
      else
      {
        product.CategoryId = category.Id;
        product.Name = item.Name;
        product.ProductType = productType;
        product.ShortDescription = item.ShortDescription;
        product.Description = item.LongDescription;
        product.Material = material;
        product.Brand = "Nha Uyen";
        product.Origin = "Viet Nam";
        product.Status = "active";
        product.IsFeatured = item.IsFeatured;
        product.UpdatedAt = DateTime.UtcNow;
      }

      foreach (var variantSeed in item.Variants)
      {
        UpsertVariant(product, variantSeed);
      }

      var defaultVariant = product.Variants.FirstOrDefault(x => x.IsDefault) ?? product.Variants.First();
      foreach (var image in item.Images)
      {
        var existingImage = product.Images.FirstOrDefault(x => x.ImageUrl == image.ImageUrl);
        if (existingImage is null)
        {
          product.Images.Add(new ProductImage
          {
            ImageUrl = image.ImageUrl,
            AltText = image.AltText,
            Variant = image.IsPrimary ? defaultVariant : null,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary
          });
          continue;
        }

        existingImage.AltText = image.AltText;
        existingImage.Variant = image.IsPrimary ? defaultVariant : null;
        existingImage.SortOrder = image.SortOrder;
        existingImage.IsPrimary = image.IsPrimary;
      }
    }

    await dbContext.SaveChangesAsync();
  }

  private async Task RemoveStaleProductsAsync()
  {
    var currentSlugs = DefaultProducts.Items.Select(x => x.Slug).ToHashSet();
    var staleProducts = await dbContext.Products
      .Where(x => x.Brand == "Nha Uyen" && !currentSlugs.Contains(x.Slug))
      .ToListAsync();

    if (staleProducts.Count == 0)
    {
      return;
    }

    dbContext.Products.RemoveRange(staleProducts);
    await dbContext.SaveChangesAsync();
  }

  private async Task RemoveStaleCategoriesAsync()
  {
    var currentSlugs = DefaultCategories.Items.Select(x => x.Slug).ToHashSet();
    var staleCategories = await dbContext.Categories
      .Where(x => !currentSlugs.Contains(x.Slug) && !x.Products.Any())
      .OrderByDescending(x => x.Parent.HasValue)
      .ToListAsync();

    if (staleCategories.Count == 0)
    {
      return;
    }

    dbContext.Categories.RemoveRange(staleCategories);
    await dbContext.SaveChangesAsync();
  }

  private static void UpsertVariant(Product product, SeedProductVariant item)
  {
    var variant = product.Variants.FirstOrDefault(x => x.Sku == item.Sku);
    if (variant is null)
    {
      product.Variants.Add(new ProductVariant
      {
        Sku = item.Sku,
        VariantName = item.VariantName,
        Size = item.Size,
        Color = item.Color,
        Price = item.Price,
        SalePrice = item.SalePrice,
        StockQty = item.StockQty,
        IsDefault = item.IsDefault,
        Status = "active"
      });

      return;
    }

    variant.VariantName = item.VariantName;
    variant.Size = item.Size;
    variant.Color = item.Color;
    variant.Price = item.Price;
    variant.SalePrice = item.SalePrice;
    variant.StockQty = item.StockQty;
    variant.IsDefault = item.IsDefault;
    variant.Status = "active";
    variant.UpdatedAt = DateTime.UtcNow;
  }

  private async Task<string> ResolveProductTypeAsync(Category category)
  {
    if (category.Slug == "phu-kien")
    {
      return "phu_kien";
    }

    if (category.Parent is null)
    {
      return "ao_dai";
    }

    var parent = await dbContext.Categories.FirstAsync(x => x.Id == category.Parent);
    return parent.Slug == "phu-kien" ? "phu_kien" : "ao_dai";
  }
}
