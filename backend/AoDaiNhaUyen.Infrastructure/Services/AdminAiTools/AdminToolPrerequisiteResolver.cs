using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services.AdminAiTools;

public sealed class AdminToolPrerequisiteResolver(
  IAdminOrderService orders,
  IAdminProductService products,
  IAdminUserService users,
  IAdminRoleService roles,
  IAdminPromoService promos)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly HashSet<string> OrderTools = new(StringComparer.Ordinal) { "confirm_order", "start_processing_order", "ship_order", "complete_order", "cancel_order", "update_order_address", "update_order_items", "delete_order", "restore_order" };
  private static readonly HashSet<string> UserTools = new(StringComparer.Ordinal) { "update_user_status", "update_user_role", "update_user_profile", "delete_user", "restore_user" };
  private static readonly HashSet<string> ProductTools = new(StringComparer.Ordinal) { "update_product", "delete_product", "restore_product", "toggle_product_status", "list_variants", "create_variant", "update_variant", "update_variant_stock", "delete_variant" };
  private static readonly HashSet<string> RoleTools = new(StringComparer.Ordinal) { "update_role", "delete_role" };
  private static readonly HashSet<string> PromoTools = new(StringComparer.Ordinal) { "get_promo_code", "update_promo_code", "toggle_promo_code", "delete_promo_code" };

  public async Task<ToolPreparationResult?> ResolveAsync(string toolName, string argsJson, CancellationToken ct)
  {
    var args = Parse(argsJson);
    if (OrderTools.Contains(toolName) && !HasGuid(args, "orderId")) return await ResolveOrderAsync(toolName, args, ct);
    if (UserTools.Contains(toolName) && !HasGuid(args, "id")) return await ResolveUserAsync(toolName, args, ct);
    if (ProductTools.Contains(toolName))
    {
      var idField = toolName is "list_variants" or "create_variant" or "update_variant" or "update_variant_stock" or "delete_variant" ? "productId" : "id";
      if (!HasGuid(args, idField)) return await ResolveProductAsync(toolName, args, idField, ct);
      if (toolName is "update_variant" or "update_variant_stock" or "delete_variant" && !HasGuid(args, "variantId"))
        return await ResolveVariantAsync(toolName, args, ct);
    }
    if (RoleTools.Contains(toolName) && !HasGuid(args, "id")) return await ResolveRoleAsync(toolName, args, ct);
    if (PromoTools.Contains(toolName) && !HasGuid(args, "promoId")) return await ResolvePromoAsync(toolName, args, ct);
    return null;
  }

  private async Task<ToolPreparationResult> ResolveOrderAsync(string toolName, Dictionary<string, object?> args, CancellationToken ct)
  {
    var code = FirstString(args, "orderCode", "code", "orderId");
    if (string.IsNullOrWhiteSpace(code)) return Ask(toolName, "Cần mã đơn AD-... hoặc orderId GUID để xác định đơn hàng.");
    var order = await orders.GetOrderByCodeAsync(code.Trim(), ct);
    if (order is null) return Ask(toolName, $"Không tìm thấy đơn hàng {code}. Vui lòng kiểm tra mã đơn.");
    args["orderId"] = order.Id.ToString(); args["orderCode"] = order.OrderCode;
    return Execute(toolName, args, $"Đã resolve đơn {order.OrderCode} ({order.OrderStatus}).");
  }

  private async Task<ToolPreparationResult> ResolveUserAsync(string toolName, Dictionary<string, object?> args, CancellationToken ct)
  {
    var search = FirstString(args, "email", "search", "userEmail", "fullName", "name");
    if (string.IsNullOrWhiteSpace(search)) return Ask(toolName, "Cần email/từ khóa người dùng để resolve userId.");
    var result = await users.GetUsersAsync(search.Trim(), 1, 10, true, ct);
    var exact = result.Items.Where(u => string.Equals(u.Email, search, StringComparison.OrdinalIgnoreCase) || string.Equals(u.FullName, search, StringComparison.OrdinalIgnoreCase)).ToList();
    var matches = exact.Count > 0 ? exact : result.Items.ToList();
    if (matches.Count == 0) return Ask(toolName, $"Không tìm thấy người dùng khớp '{search}'.");
    if (matches.Count > 1) return Ask(toolName, $"Tìm thấy nhiều người dùng khớp '{search}'. Vui lòng chọn email/tên cụ thể.");
    args["id"] = matches[0].Id.ToString();
    return Execute(toolName, args, $"Đã resolve người dùng {matches[0].FullName} ({matches[0].Email ?? "không email"}).");
  }

  private async Task<ToolPreparationResult> ResolveProductAsync(string toolName, Dictionary<string, object?> args, string idField, CancellationToken ct)
  {
    var search = FirstString(args, "sku", "search", "productName", "name", "product");
    if (string.IsNullOrWhiteSpace(search)) return Ask(toolName, "Cần tên/SKU/từ khóa sản phẩm để resolve productId.");
    var (items, _) = await products.GetPagedAsync(search.Trim(), null, 1, 10, true, ct);
    var exact = items.Where(p => string.Equals(p.Name, search, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Slug, search, StringComparison.OrdinalIgnoreCase)).ToList();
    var matches = exact.Count > 0 ? exact : items.ToList();
    if (matches.Count == 0) return Ask(toolName, $"Không tìm thấy sản phẩm khớp '{search}'.");
    if (matches.Count > 1) return Ask(toolName, $"Tìm thấy nhiều sản phẩm khớp '{search}'. Vui lòng chọn tên/SKU cụ thể.");
    args[idField] = matches[0].Id.ToString();
    return Execute(toolName, args, $"Đã resolve sản phẩm {matches[0].Name}.");
  }

  private async Task<ToolPreparationResult> ResolveVariantAsync(string toolName, Dictionary<string, object?> args, CancellationToken ct)
  {
    if (!TryGetGuid(args, "productId", out var productId))
      return Ask(toolName, "Cần productId GUID trước khi resolve variantId từ SKU.");

    var sku = FirstString(args, "sku", "variantSku", "variantId");
    var variantName = FirstString(args, "variantName", "size", "color");
    if (string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(variantName))
      return Ask(toolName, "Cần SKU hoặc thông tin biến thể để resolve variantId.");

    var product = await products.GetByIdAsync(productId, ct);
    if (product is null)
      return Ask(toolName, "Không tìm thấy sản phẩm để resolve biến thể.");

    var matches = product.Variants.Where(v =>
      (!string.IsNullOrWhiteSpace(sku) && v.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase))
      || (!string.IsNullOrWhiteSpace(variantName) && string.Equals(v.VariantName, variantName.Trim(), StringComparison.OrdinalIgnoreCase)))
      .ToList();

    if (matches.Count == 0)
      return Ask(toolName, $"Không tìm thấy biến thể SKU '{sku ?? variantName}' trong sản phẩm {product.Name}.");
    if (matches.Count > 1)
      return Ask(toolName, $"Tìm thấy nhiều biến thể khớp '{sku ?? variantName}' trong sản phẩm {product.Name}. Vui lòng chọn SKU cụ thể.");

    args["variantId"] = matches[0].Id.ToString();
    args["sku"] = matches[0].Sku;
    return Execute(toolName, args, $"Đã resolve biến thể SKU {matches[0].Sku} của sản phẩm {product.Name}.");
  }

  private async Task<ToolPreparationResult> ResolveRoleAsync(string toolName, Dictionary<string, object?> args, CancellationToken ct)
  {
    var roleName = FirstString(args, "roleName", "role", "name");
    if (string.IsNullOrWhiteSpace(roleName)) return Ask(toolName, "Cần tên vai trò để resolve roleId.");
    var matches = (await roles.GetRolesAsync(ct)).Where(r => r.Name.Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 0) return Ask(toolName, $"Không tìm thấy vai trò '{roleName}'.");
    if (matches.Count > 1) return Ask(toolName, $"Có nhiều vai trò khớp '{roleName}'. Vui lòng chọn rõ hơn.");
    args["id"] = matches[0].Id.ToString();
    return Execute(toolName, args, $"Đã resolve vai trò {matches[0].Name}.");
  }

  private async Task<ToolPreparationResult> ResolvePromoAsync(string toolName, Dictionary<string, object?> args, CancellationToken ct)
  {
    var code = FirstString(args, "code", "promoCode", "search");
    if (string.IsNullOrWhiteSpace(code)) return Ask(toolName, "Cần mã khuyến mãi để resolve promoId.");
    var matches = (await promos.GetAllAsync(ct)).Where(p => p.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 0) return Ask(toolName, $"Không tìm thấy mã khuyến mãi '{code}'.");
    if (matches.Count > 1) return Ask(toolName, $"Có nhiều mã khuyến mãi khớp '{code}'. Vui lòng chọn rõ hơn.");
    args["promoId"] = matches[0].Id.ToString();
    return Execute(toolName, args, $"Đã resolve mã khuyến mãi {matches[0].Code}.");
  }

  private static ToolPreparationResult Execute(string toolName, Dictionary<string, object?> args, string summary) => new(ToolPreparationAction.Execute, toolName, JsonSerializer.Serialize(args, JsonOptions), null, summary);
  private static ToolPreparationResult Ask(string toolName, string message) => new(ToolPreparationAction.AskClarification, toolName, null, message, message, true);
  private static Dictionary<string, object?> Parse(string json) => JsonSerializer.Deserialize<Dictionary<string, object?>>(string.IsNullOrWhiteSpace(json) ? "{}" : json, JsonOptions) ?? [];
  private static bool TryGetGuid(Dictionary<string, object?> args, string name, out Guid id)
  {
    id = default;
    return args.TryGetValue(name, out var value) && value is not null && Guid.TryParse(value.ToString(), out id);
  }
  private static bool HasGuid(Dictionary<string, object?> args, string name) => args.TryGetValue(name, out var value) && value is not null && Guid.TryParse(value.ToString(), out _);
  private static string? FirstString(Dictionary<string, object?> args, params string[] names)
  {
    foreach (var name in names)
      if (args.TryGetValue(name, out var value) && value is not null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString();
    return null;
  }
}
