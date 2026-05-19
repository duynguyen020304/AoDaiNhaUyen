# Áo Dài Nhã Uyên (https://aodainhauyen.io.vn)

Nền tảng thương mại điện tử áo dài Việt Nam: backend ASP.NET Core 10 + PostgreSQL, frontend React 19 + TypeScript + Vite. Chức năng chính: danh mục sản phẩm, giỏ hàng/thanh toán, đăng nhập Google, AI try-on, chat tư vấn.

## Yêu cầu hệ thống

- .NET SDK 10
- PostgreSQL 15+
- Bun 1.x
- Node.js tương thích Vite 8
- Git

## Cấu hình môi trường

### Backend

Tạo file `backend/AoDaiNhaUyen.Api/.env` từ mẫu:

```bash
cp backend/AoDaiNhaUyen.Api/.env.example backend/AoDaiNhaUyen.Api/.env
```

Biến quan trọng:

| Biến | Mục đích | Ví dụ local |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | Chuỗi kết nối PostgreSQL | `Host=localhost;Port=5432;Database=aodai_nha_uyen;Username=postgres;Password=postgres` |
| `ASPNETCORE_ENVIRONMENT` | Môi trường chạy | `Development` |
| `JwtSettings__SecretKey` | Khóa ký JWT | chuỗi dài, riêng tư |
| `EmailSettings__*` | SMTP gửi email | Gmail SMTP/app password |
| `GoogleOAuth__*` | Google OAuth | client id/secret/redirect URI |
| `ZaloOAuth__*` | Zalo OAuth | app id/secret/redirect URI |
| `GoogleCloud__*` | AI try-on/stylist | project/API key/model |

`appsettings.Development.json` bật `RunMigrationsAndSeedOnStartup=true`, nên API có thể tự migrate + seed khi chạy.

### Frontend

Tạo file `frontend/.env` từ mẫu:

```bash
cp frontend/.env.example frontend/.env
```

Biến frontend:

| Biến | Mục đích | Ví dụ local |
| --- | --- | --- |
| `VITE_API_BASE_URL` | URL backend API | `http://localhost:5043` |
| `PUBLIC_BACKEND_DOMAIN` | Domain backend fallback | `http://localhost:5043` |
| `PUBLIC_GOOGLE_CLIENT_ID` | Google OAuth client id | lấy từ Google Cloud |
| `PUBLIC_ZALO_APP_ID` | Zalo app id | lấy từ Zalo Developers |

Vite chỉ expose biến có prefix `VITE_` hoặc `PUBLIC_`.

## Cài đặt local

### 1. Chuẩn bị database

```bash
createdb -h localhost -p 5432 -U postgres aodai_nha_uyen
```

Nếu DB đã tồn tại, bỏ qua bước này.

### 2. Cài backend

```bash
cd backend
dotnet restore
dotnet build
```

Chạy migration + seed bằng API startup:

```bash
cd backend/AoDaiNhaUyen.Api
dotnet run
```

API mặc định dùng `http://localhost:5043` khi chạy theo launch profile/dev config.

### 3. Cài frontend

```bash
cd frontend
bun install
bun run dev
```

Frontend dev server mặc định: `http://localhost:5173`.

## Lệnh thường dùng

### Backend

```bash
cd backend
dotnet build
dotnet test
```

Thêm migration mới:

```bash
cd backend/AoDaiNhaUyen.Infrastructure
dotnet ef migrations add <TenMigration> --startup-project ../AoDaiNhaUyen.Api
```

Update database thủ công:

```bash
cd backend/AoDaiNhaUyen.Infrastructure
dotnet ef database update --startup-project ../AoDaiNhaUyen.Api
```

### Frontend

```bash
cd frontend
bun run lint
bun run build
bun run preview
```

Build output: `frontend/dist/`.

## Triển khai

### Backend

1. Cấu hình biến môi trường production tương ứng file `.env`.
2. Dùng PostgreSQL production, không dùng credential local.
3. Chạy migration trước hoặc bật `RunMigrationsAndSeedOnStartup` có kiểm soát.
4. Publish API:

```bash
cd backend/AoDaiNhaUyen.Api
dotnet publish -c Release -o publish
```

5. Serve API sau reverse proxy HTTPS, cấu hình CORS `FrontendOrigins` đúng domain frontend.

### Frontend

1. Cập nhật `frontend/.env` với API/OAuth production.
2. Build static SPA:

```bash
cd frontend
bun install
bun run build
```

3. Deploy nội dung `frontend/dist/` lên static host.
4. Bắt buộc cấu hình SPA fallback: mọi route trả về `index.html`.
5. Preview trước khi publish:

```bash
bun run preview
```

## Kiểm tra trước khi bàn giao

```bash
cd backend && dotnet test
cd ../frontend && bun run lint && bun run build
```

Với thay đổi UI, kiểm tra thêm trên trình duyệt qua Playwright/visual QA.

## Ghi chú kỹ thuật

- Backend clean architecture: Api → Application → Infrastructure; Domain độc lập.
- API response dùng envelope `{ success, message, data, errors, timestamp }`.
- Frontend dùng CSS Modules + PostCSS, không dùng Tailwind.
- Design tokens nằm ở `frontend/src/styles/variables.css`.
- Toàn bộ UI/API message hướng người dùng dùng tiếng Việt.
