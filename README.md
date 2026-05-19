# Áo Dài Nhã Uyên (https://aodainhauyen.io.vn)

Nền tảng thương mại điện tử áo dài Việt Nam: ASP.NET Core 10 API + PostgreSQL + React 19/Vite SPA. Chức năng chính: catalog sản phẩm, giỏ hàng/thanh toán, đăng nhập Google/Zalo, AI try-on, chat tư vấn.

## Yêu cầu

- .NET SDK 10
- PostgreSQL 15+
- Bun 1.x
- Node.js tương thích Vite 8
- Git

## Cấu hình môi trường

### Backend

```bash
cp backend/AoDaiNhaUyen.Api/.env.example backend/AoDaiNhaUyen.Api/.env
```

Cấu hình chính trong `backend/AoDaiNhaUyen.Api/.env`:

| Biến | Mục đích | Local mặc định |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Môi trường ASP.NET Core | `Development` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL | `Host=localhost;Port=5432;Database=aodai_nha_uyen;Username=postgres;Password=postgres` |
| `JwtSettings__SecretKey` | Khóa ký JWT | đổi thành chuỗi dài, riêng tư |
| `FrontendOrigins` | CORS cho frontend dev/preview | localhost/127.0.0.1 ports `5173`, `4173` |
| `RunMigrationsAndSeedOnStartup` | Tự migrate + seed khi API chạy | `true` cho local |
| `EmailSettings__*` | SMTP gửi email | điền khi cần email thật |
| `GoogleOAuth__*` | Google OAuth | client id/secret/redirect URI |
| `ZaloOAuth__*` | Zalo OAuth | app id/secret/redirect URI |
| `GoogleCloud__*` | AI try-on/stylist | project/API key/model |
| `CacheSettings__Version` | Cache/static version | `v1` |

Không commit file `.env` thật.

### Frontend

```bash
cp frontend/.env.example frontend/.env
```

Cấu hình chính trong `frontend/.env`:

| Biến | Mục đích | Local mặc định |
| --- | --- | --- |
| `VITE_API_BASE_URL` | URL backend API cho fetch client | `http://localhost:5043` |
| `PUBLIC_BACKEND_DOMAIN` | Fallback backend domain | `http://localhost:5043` |
| `PUBLIC_GOOGLE_CLIENT_ID` | Google OAuth public client id | để trống nếu chưa dùng OAuth |
| `PUBLIC_ZALO_APP_ID` | Zalo public app id | để trống nếu chưa dùng OAuth |

Vite chỉ expose biến có prefix `VITE_` hoặc `PUBLIC_`. Không đặt secret frontend.

## Cài đặt local

### 1. Tạo database

```bash
createdb -h localhost -p 5432 -U postgres aodai_nha_uyen
```

Nếu password/user khác mẫu, sửa `ConnectionStrings__DefaultConnection` trong backend `.env`.

### 2. Backend

```bash
cd backend
dotnet restore
dotnet build
```

Chạy API + tự migrate/seed theo `.env`:

```bash
cd backend/AoDaiNhaUyen.Api
dotnet run
```

Migrate thủ công nếu cần:

```bash
cd backend/AoDaiNhaUyen.Infrastructure
ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=aodai_nha_uyen;Username=postgres;Password=postgres' dotnet ef database update --startup-project ../AoDaiNhaUyen.Api
```

API local: `http://localhost:5043`.

### 3. Frontend

```bash
cd frontend
bun install
bun run dev
```

Frontend dev: `http://localhost:5173`.

## Lệnh thường dùng

### Backend

```bash
cd backend
dotnet build
dotnet test
```

Thêm migration:

```bash
cd backend/AoDaiNhaUyen.Infrastructure
dotnet ef migrations add <TenMigration> --startup-project ../AoDaiNhaUyen.Api
```

Update database:

```bash
cd backend/AoDaiNhaUyen.Infrastructure
dotnet ef database update --startup-project ../AoDaiNhaUyen.Api
```

### Frontend

Dùng Bun vì `bun.lock` tồn tại.

```bash
cd frontend
bun install
bun run lint
bun run build
bun run preview
```

Build output: `frontend/dist/`. SPA dùng BrowserRouter, host production phải fallback mọi route về `index.html`.

## Triển khai

### Backend

1. Tạo biến môi trường production tương ứng `backend/AoDaiNhaUyen.Api/.env.example`.
2. Dùng PostgreSQL production, không dùng credential local.
3. Cấu hình `FrontendOrigins` đúng domain frontend.
4. Chạy migration trước deploy, hoặc bật `RunMigrationsAndSeedOnStartup` có kiểm soát.
5. Publish:

```bash
cd backend/AoDaiNhaUyen.Api
dotnet publish -c Release -o publish
```

6. Chạy API sau reverse proxy HTTPS.

### Frontend

1. Cập nhật `frontend/.env` với API/OAuth production.
2. Build static SPA:

```bash
cd frontend
bun install
bun run build
```

3. Deploy nội dung `frontend/dist/` lên static host.
4. Bật SPA fallback về `index.html`.
5. Preview trước publish:

```bash
bun run preview
```

## Kiểm tra trước bàn giao

```bash
cd backend && dotnet test
cd ../frontend && bun run lint && bun run build
```

Thay đổi UI cần kiểm tra trình duyệt/visual QA.

## Ghi chú kỹ thuật

- Backend clean architecture: Api → Application → Infrastructure; Domain độc lập.
- API response envelope: `{ success, message, data, errors, timestamp }`.
- Frontend: React 19, react-router-dom 7, TypeScript 6, Vite 8.
- Package manager frontend: Bun.
- CSS Modules + PostCSS; không dùng Tailwind.
- Design tokens: `frontend/src/styles/variables.css`.
- Service Worker: `frontend/public/sw.js`; Vite middleware set `/sw.js` no-store.
- UI/API message hướng người dùng: tiếng Việt.
