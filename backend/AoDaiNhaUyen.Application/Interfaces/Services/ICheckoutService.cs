using AoDaiNhaUyen.Application.DTOs.Auth;
using AoDaiNhaUyen.Application.DTOs.Checkout;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface ICheckoutService
{
  Task<AuthResult<CheckoutResultDto>> CheckoutAsync(long userId, CheckoutRequestDto request, CancellationToken cancellationToken = default);
}
