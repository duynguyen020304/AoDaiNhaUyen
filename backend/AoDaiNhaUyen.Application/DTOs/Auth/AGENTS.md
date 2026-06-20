<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Auth

## Purpose
Authentication and authorization DTOs covering JWT token results, session state, authenticated user info, social OAuth user profiles (Google, Zalo), and token validation outcomes. Used by `IAuthService` and the Auth API controller.

## Key Files
| File | Description |
|------|-------------|
| `AuthResult.cs` | Top-level auth result: `Succeeded`, `Value` (AccessToken + RefreshToken + `AuthUserDto`), `ErrorCode`, `ErrorMessage` |
| `AuthSessionDto.cs` | Session token data: `Token`, `ExpiresAt` — issued as cookie or response body |
| `AuthUserDto.cs` | Authenticated user projection: `Id`, `FullName`, `Email`, `Phone`, `Roles` |
| `GoogleUserInfoDto.cs` | Google OAuth user: `Subject`, `Email`, `EmailVerified`, `Name`, `Picture` |
| `ZaloUserInfoDto.cs` | Zalo OAuth user: `Id`, `Name`, `Picture` |
| `TokenValidationResult.cs` | Token validation outcome for email verification and password reset flows: status enum + metadata |

## For AI Agents

### Working In This Directory
- `AuthResult` follows the shared result pattern: `{ Succeeded, Value, ErrorCode, ErrorMessage }`
- `AuthUserDto` is the user shape embedded in the JWT claims and returned on login/refresh — keep it lean
- Social OAuth DTOs (`GoogleUserInfoDto`, `ZaloUserInfoDto`) are populated by the respective OAuth services and never persisted directly; they drive user upsert logic in `IAuthService`
- `TokenValidationResult` covers both email-verification and password-reset tokens (distinguished by a status enum)

### Common Patterns
```csharp
// Successful login:
return new AuthResult(true, new AuthSessionData(accessToken, refreshToken, userDto));

// Failed login:
return new AuthResult(false, null, "INVALID_CREDENTIALS", "Email or password is incorrect");
```

## Dependencies
### Internal
- `IAuthService` — consumes and returns these DTOs
- `IGoogleOAuthService` / `IZaloOAuthService` — return `GoogleUserInfoDto` / `ZaloUserInfoDto`
- `IJwtTokenService` — produces tokens embedded in `AuthResult.Value`

<!-- MANUAL: -->
