# Enterprise Operations Hub

<p align="center">
  <strong>Dashboard-X</strong> — Hệ thống quản lý và giám sát hoạt động doanh nghiệp toàn diện.<br>
  Xây dựng trên <strong>ASP.NET Core 9 MVC</strong> + <strong>SQL Server</strong> + <strong>SignalR</strong> + <strong>Hangfire</strong> + <strong>AI Chat Assistant</strong>.
</p>

## Mục lục

- [Giới thiệu](#giới-thiệu)
- [Tính năng chính](#tính-năng-chính)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt](#cài-đặt)
- [Cấu trúc dự án](#cấu-trúc-dự-án)
- [Tài khoản mặc định](#tài-khoản-mặc-định)
- [Các module](#các-module)
- [Danh sách entity](#danh-sách-entity)
- [AI Chat Assistant](#ai-chat-assistant)
- [Đặc biệt](#đặc-biệt)

---

## Giới thiệu

**Dashboard-X** (tên gốc: TradingServiceDashboard) là một hệ thống dashboard doanh nghiệp được xây dựng bằng ASP.NET Core 9 MVC, hỗ trợ quản lý đa ngành: Bán hàng, Marketing, Kho vận, Tài chính, Nhân sự, Chăm sóc khách hàng và điều hành.

Hệ thống sử dụng **SQL Server** làm cơ sở dữ liệu, **Entity Framework Core** cho ORM, **ASP.NET Identity** cho xác thực phân quyền, **SignalR** cho thông báo thời gian thực, **Hangfire** cho các job nền tự động, và **AI Chat Assistant** (GPT-4o-mini) để hỗ trợ phân tích dữ liệu tự động.

---

## Tính năng chính

- **7 module phân quyền theo vai trò** — Executive, Sales, Marketing, Inventory, Finance, HR, Customer Service
- **Dashboard tổng quan** cho từng module với biểu đồ ApexCharts
- **51 entity CRUD** — quản lý qua generic `CrudService<T>` registry
- **AI Chat Assistant thông minh** — trợ lý AI trong từng module, trả lời dựa trên dữ liệu thực tế của doanh nghiệp, hỗ trợ streaming real-time
- **Thông báo thời gian thực** qua SignalR hub
- **Job nền tự động** qua Hangfire (tổng hợp dữ liệu, thông báo, reminder)
- **Import/Export Excel** cho tất cả bảng dữ liệu
- **Identity** — xác thực, phân quyền, khóa tài khoản, quản lý user
- **Auto-seed** — dữ liệu mẫu và tài khoản được tạo tự động khi khởi chạy
- **Tìm kiếm toàn cầu** — Global search across all entities (Text2SQL)
- **PDF Report** — Export báo cáo ra PDF qua QuestPDF

---

## Yêu cầu hệ thống

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (LocalDB, Express hoặc bản đầy đủ)
- Trình duyệt hiện đại (Chrome, Edge, Firefox)
- OpenAI-compatible API endpoint cho AI Chat (cấu hình trong `appsettings.json`)

---

## Cài đặt

### 1. Clone và mở project

```bash
cd TradingServiceDashboard
```

### 2. Cấu hình chuỗi kết nối

Sửa `appsettings.json` hoặc `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=TradingServiceDashboard;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### 3. Cấu hình AI Chat (tùy chọn)

```json
"AIChat": {
  "Endpoint": "https://routerapi.vovantin.online/v1/chat/completions",
  "ApiKey": "your-api-key",
  "Model": "gpt-4o-mini",
  "MaxTokens": "4000",
  "MaxHistoryMessages": "10"
}
```

### 4. Chạy ứng dụng

```bash
dotnet run
```

- Hệ thống sẽ tự động tạo database, chạy migration và seed dữ liệu mẫu + tài khoản.
- Hangfire dashboard: `http://localhost:<port>/hangfire`
- SignalR hub: `/notificationHub`
- AI Chat hub: `/aiChatHub`

### 5. Truy cập

Mở trình duyệt tại `http://localhost:<port>` và đăng nhập bằng tài khoản bên dưới.

---

## Cấu trúc dự án

```
TradingServiceDashboard/
├── Controllers/              # MVC controllers (25 controllers)
├── Models/
│   ├── *.cs                 # 51 entity models
│   ├── ViewModels/          # View-specific models (DTOs)
│   └── SD.cs                # Role & URL constants
├── Views/
│   ├── Shared/              # Layout, Partial, Navbar, Footer
│   ├── Auth/                # Login, Register, Forgot Password
│   ├── Executive/          # Dashboard + CRUD
│   ├── Sales/              # Dashboard + CRUD
│   ├── Marketing/          # Dashboard + CRUD
│   ├── Inventory/          # Dashboard + CRUD
│   ├── Finance/            # Dashboard + CRUD
│   ├── HumanResources/      # Dashboard + CRUD
│   ├── CustomerService/    # Dashboard + CRUD
│   └── AIAssistant/        # AI Chat Assistant standalone page
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs         # 50+ seed methods
│   └── RoleSeeder.cs       # Roles & user accounts
├── Services/
│   ├── Dashboard/          # Dashboard data services (7 modules)
│   ├── Interfaces/         # Service interfaces
│   ├── Crud/               # Generic CRUD service registry
│   ├── ExcelCrudService.cs  # Excel import/export
│   ├── AIChatService.cs     # AI chat (OpenAI-compatible API)
│   ├── AIContextAggregator.cs # Context aggregation per department
│   ├── TextToSqlService.cs  # Text-to-SQL query generation (Global Search)
│   ├── PdfReportService.cs  # PDF report generation (QuestPDF)
│   ├── DatabaseSchemaService.cs # Database schema introspection
│   └── SqlValidationService.cs # SQL query validation
├── Jobs/
│   └── NotificationJobs.cs  # Hangfire background jobs
├── Hubs/
│   ├── NotificationHub.cs   # SignalR real-time notifications
│   └── AIChatHub.cs         # SignalR AI chat streaming
├── Migrations/              # EF Core migrations
└── wwwroot/
    ├── css/                 # Custom styles (site.css, ai-assistant-chatgpt.css)
    ├── js/                  # Client-side scripts (dashboard, AI chat, form handling)
    ├── vendor/              # Third-party libs (Bootstrap, ApexCharts, jQuery, etc.)
    └── assets/              # Images, fonts
```

---

## Tài khoản mặc định

| Email | Mật khẩu | Vai trò |
|-------|----------|---------|
| `executive@company.com` | `Executive123!` | Executive |
| `sales@company.com` | `Sales123!` | Sales |
| `marketing@company.com` | `Marketing123!` | Marketing |
| `inventory@company.com` | `Inventory123!` | Inventory |
| `finance@company.com` | `Finance123!` | Finance |
| `hr@company.com` | `Hr123456!` | Human Resources |
| `cskh@company.com` | `Cskh123456!` | Customer Service |

> **Lưu ý:** Sau khi đăng nhập thành công, hệ thống sẽ tự động chuyển hướng đến dashboard tương ứng với vai trò của bạn.

---

## Các module

### Executive
Tổng quan toàn doanh nghiệp — Regions, Branches, Departments.

### Sales
Quản lý bán hàng — Customers, Customer Groups, Sales Channels, Opportunities, Quotes, Sales Orders, Invoices, Returns, Payments.

### Marketing
Quản lý marketing — Campaigns, Leads, Marketing Spend.

### Inventory
Quản lý kho vận — Products, Categories, Warehouses, Suppliers, Purchase Orders, Receipts, Stock Levels.

### Finance
Quản lý tài chính — Expenses, Expense Categories.

### Human Resources
Quản lý nhân sự — Employees, Positions, Payroll, Leave Requests, Performance Reviews, Job Openings, Applicants, Attendance.

### Customer Service
Quản lý chăm sóc khách hàng — Support Tickets.

---

## Danh sách entity

| # | Entity | Module |
|---|--------|-------|
| 1 | Region | Executive |
| 2 | Branch | Executive |
| 3 | Department | Executive |
| 4 | Customer | Sales |
| 5 | CustomerGroup | Sales |
| 6 | SalesChannel | Sales |
| 7 | Opportunity | Sales |
| 8 | OpportunityStage | Sales |
| 9 | OpportunityStageHistory | Sales |
| 10 | Quote | Sales |
| 11 | SalesOrder | Sales |
| 12 | SalesOrderDetail | Sales |
| 13 | SalesInvoice | Sales |
| 14 | SalesInvoiceDetail | Sales |
| 15 | SalesReturn | Sales |
| 16 | SalesReturnDetail | Sales |
| 17 | CustomerPayment | Sales |
| 18 | MarketingCampaign | Marketing |
| 19 | MarketingLead | Marketing |
| 20 | MarketingSpendDaily | Marketing |
| 21 | Product | Inventory |
| 22 | ProductCategory | Inventory |
| 23 | Warehouse | Inventory |
| 24 | Supplier | Inventory |
| 25 | PurchaseOrder | Inventory |
| 26 | PurchaseOrderDetail | Inventory |
| 27 | PurchaseReceipt | Inventory |
| 28 | PurchaseReceiptDetail | Inventory |
| 29 | PurchaseInvoice | Inventory |
| 30 | PurchaseInvoiceDetail | Inventory |
| 31 | SupplierPayment | Inventory |
| 32 | Inventory | Inventory |
| 33 | InventorySnapshot | Inventory |
| 34 | StockTransaction | Inventory |
| 35 | Expense | Finance |
| 36 | ExpenseCategory | Finance |
| 37 | Employee | HR |
| 38 | Position | HR |
| 39 | Payroll | HR |
| 40 | LeaveRequest | HR |
| 41 | PerformanceReview | HR |
| 42 | JobOpening | HR |
| 43 | Applicant | HR |
| 44 | Attendance | HR |
| 45 | SupportTicket | Customer Service |
| 46 | Notification | System |
| 47 | NotificationConfig | System |
| 48 | DimDate | BI/Dashboard |
| 49 | KpiTarget | BI/Dashboard |
| 50 | AIChatSession | AI Chat |
| 51 | AIChatMessage | AI Chat |

---

## AI Chat Assistant

### Tổng quan

AI Chat Assistant là trợ lý thông minh tích hợp sâu vào từng module của hệ thống. Mỗi khi người dùng chat, AI sẽ được cung cấp context bao gồm:

- **KPI Summary** — Tổng hợp các chỉ số KPI của module (doanh số, chi phí, tồn kho...)
- **Top Items** — Top sản phẩm, khách hàng, nhân viên xuất sắc
- **Recent Data** — Dữ liệu và hoạt động gần nhất
- **Chart Summary** — Tóm tắt xu hướng từ các biểu đồ dashboard

### Tính năng

- **Streaming real-time** — Phản hồi AI hiển thị từng token trong khi đang generate (SignalR streaming)
- **Multi-department** — Chuyên gia AI riêng cho từng module: Sales, Finance, Marketing, Inventory, HR, CSKH, Executive
- **Session management** — Lưu lịch sử chat theo phiên, hỗ trợ đa phiên
- **Context-aware** — AI đọc dữ liệu thực tế từ database và đưa ra insights có số liệu cụ thể
- **Quick actions** — Câu hỏi gợi ý nhanh được custom theo từng department
- **Caching** — Context data được cache 5 phút để tối ưu hiệu năng
- **Widget nhúng** — AI Chat widget có thể nhúng vào bất kỳ trang dashboard nào
- **Trang standalone** — `/AIAssistant` — Giao diện ChatGPT-like riêng biệt

### Cấu trúc kỹ thuật

| File | Mô tả |
|------|--------|
| `AIChatService.cs` | Xử lý chat logic, gọi LLM API, quản lý session/message |
| `AIContextAggregator.cs` | Tổng hợp context (KPI, top items, chart data) theo department |
| `AIChatHub.cs` | SignalR hub cho streaming real-time |
| `AIChatController.cs` | REST API: sessions, messages, departments |
| `AIAssistantController.cs` | MVC controller cho trang `/AIAssistant` |
| `AIChatSession.cs` | Entity lưu session chat |
| `AIChatMessage.cs` | Entity lưu từng message |
| `_AIChatWidget.cshtml` | Widget có thể nhúng vào layout |
| `ai-chat-client.js` | Client-side SignalR cho AI chat |
| `ai-assistant-chatgpt.css` | Styles giao diện ChatGPT-like |

### API Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|--------|
| POST | `/api/aichat/sessions` | Tạo phiên chat mới |
| GET | `/api/aichat/sessions` | Lấy danh sách phiên |
| GET | `/api/aichat/sessions/{id}/messages` | Lấy lịch sử tin nhắn |
| DELETE | `/api/aichat/sessions/{id}` | Xóa phiên chat |
| GET | `/api/aichat/departments` | Lấy danh sách departments |

### SignalR Events

| Event | Chiều | Mô tả |
|-------|--------|--------|
| `SendMessage` | Client → Server | Gửi tin nhắn |
| `ReceiveChunk` | Server → Client | Nhận từng token (streaming) |
| `StreamComplete` | Server → Client | Kết thúc stream |
| `ReceiveError` | Server → Client | Thông báo lỗi |
| `TypingIndicator` | Server → Client | Trạng thái đang typing |

---

## Đặc biệt

- **RoleSeeder** — Tạo 7 vai trò + 7 tài khoản mặc định tự động khi khởi chạy.
- **DbSeeder** — Seed 51 bảng dữ liệu mẫu (regions, branches, employees, customers, products, orders, invoices, marketing, HR...).
- **Generic CRUD** — `CrudServiceRegistry` quản lý CRUD cho 51 entity types qua 7 controller.
- **Excel Import/Export** — Mỗi module hỗ trợ tải lên và tải xuống file Excel.
- **Thông báo thời gian thực** — SignalR hub gửi notification trực tiếp đến trình duyệt.
- **Background Jobs** — Hangfire chạy tổng hợp dữ liệu định kỳ cho từng module.
- **Authorization Policies** — 8 policy riêng biệt giới hạn quyền truy cập theo vai trò.
- **AI Chat Assistant** — Trợ lý AI thông minh với context-aware, streaming real-time, multi-department expertise.

---

*Dashboard-X — Enterprise Operations Hub*
