using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.DTOs.Cart;
using AoDaiNhaUyen.Application.Interfaces.Repositories;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CartService(ICartRepository cartRepository) : ICartService
{
  public async Task<AuthResult<CartDto>> GetCartAsync(long userId, CancellationToken cancellationToken = default)
  {
    var cart = await EnsureCartLoadedAsync(userId, cancellationToken);
    return AuthResult<CartDto>.Success(cartRepository.MapCart(cart));
  }

  public async Task<AuthResult<CartDto>> AddItemAsync(long userId, AddCartItemDto request, CancellationToken cancellationToken = default)
  {
    if (request.Quantity <= 0)
    {
      return AuthResult<CartDto>.Failure("invalid_quantity", "Số lượng phải lớn hơn 0.");
    }

    var variant = await cartRepository.GetVariantForCartAsync(request.VariantId, cancellationToken);
    if (variant is null || variant.Product.Status != "active" || variant.Status != "active")
    {
      return AuthResult<CartDto>.Failure("variant_not_found", "Biến thể sản phẩm không khả dụng.");
    }

    var cart = await EnsureCartLoadedAsync(userId, cancellationToken);
    var existingItem = cart.Items.FirstOrDefault(item => item.VariantId == request.VariantId);
    var nextQuantity = request.Quantity + (existingItem?.Quantity ?? 0);

    if (variant.StockQty < nextQuantity)
    {
      return AuthResult<CartDto>.Failure("insufficient_stock", "Số lượng tồn kho không đủ.");
    }

    if (existingItem is null)
    {
      cart.Items.Add(new CartItem
      {
        CartId = cart.Id,
        VariantId = variant.Id,
        Quantity = request.Quantity,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Variant = variant
      });
    }
    else
    {
      existingItem.Quantity = nextQuantity;
      existingItem.UpdatedAt = DateTime.UtcNow;
    }

    cart.UpdatedAt = DateTime.UtcNow;
    await cartRepository.SaveChangesAsync(cancellationToken);

    cart = await EnsureCartLoadedAsync(userId, cancellationToken);
    return AuthResult<CartDto>.Success(cartRepository.MapCart(cart));
  }

  public async Task<AuthResult<CartDto>> UpdateItemAsync(long userId, long itemId, UpdateCartItemDto request, CancellationToken cancellationToken = default)
  {
    if (request.Quantity <= 0)
    {
      return AuthResult<CartDto>.Failure("invalid_quantity", "Số lượng phải lớn hơn 0.");
    }

    var item = await cartRepository.GetItemByIdAsync(userId, itemId, cancellationToken);
    if (item is null)
    {
      return AuthResult<CartDto>.Failure("cart_item_not_found", "Không tìm thấy sản phẩm trong giỏ hàng.");
    }

    if (item.Variant.Product.Status != "active" || item.Variant.Status != "active")
    {
      return AuthResult<CartDto>.Failure("variant_not_available", "Biến thể sản phẩm không còn khả dụng.");
    }

    if (item.Variant.StockQty < request.Quantity)
    {
      return AuthResult<CartDto>.Failure("insufficient_stock", "Số lượng tồn kho không đủ.");
    }

    item.Quantity = request.Quantity;
    item.UpdatedAt = DateTime.UtcNow;
    item.Cart.UpdatedAt = DateTime.UtcNow;
    await cartRepository.SaveChangesAsync(cancellationToken);

    var cart = await EnsureCartLoadedAsync(userId, cancellationToken);
    return AuthResult<CartDto>.Success(cartRepository.MapCart(cart));
  }

  public async Task<AuthResult<CartDto>> RemoveItemAsync(long userId, long itemId, CancellationToken cancellationToken = default)
  {
    var item = await cartRepository.GetItemByIdAsync(userId, itemId, cancellationToken);
    if (item is null)
    {
      return AuthResult<CartDto>.Failure("cart_item_not_found", "Không tìm thấy sản phẩm trong giỏ hàng.");
    }

    item.Cart.Items.Remove(item);
    item.Cart.UpdatedAt = DateTime.UtcNow;
    await cartRepository.SaveChangesAsync(cancellationToken);

    var cart = await EnsureCartLoadedAsync(userId, cancellationToken);
    return AuthResult<CartDto>.Success(cartRepository.MapCart(cart));
  }

  public async Task<AuthResult<bool>> ClearCartAsync(long userId, CancellationToken cancellationToken = default)
  {
    var cart = await EnsureCartLoadedAsync(userId, cancellationToken);
    cart.Items.Clear();
    cart.UpdatedAt = DateTime.UtcNow;
    await cartRepository.SaveChangesAsync(cancellationToken);
    return AuthResult<bool>.Success(true);
  }

  private async Task<Cart> EnsureCartLoadedAsync(long userId, CancellationToken cancellationToken)
  {
    var cart = await cartRepository.GetByUserIdWithItemsAsync(userId, cancellationToken);
    if (cart is not null)
    {
      return cart;
    }

    await cartRepository.CreateForUserAsync(userId, cancellationToken);
    return (await cartRepository.GetByUserIdWithItemsAsync(userId, cancellationToken))!;
  }
}
