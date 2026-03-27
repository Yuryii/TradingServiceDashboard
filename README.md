# Enterprise Operations Hub

<p align="center">
  <strong>Dashboard-X</strong> — Hệ thống quản lý và giám sát hoạt động doanh nghiệp toàn diện.<br>
  Xây dựng trên <strong>ASP.NET Core 8 MVC</strong> + <strong>SQL Server</strong> + <strong>SignalR</strong> + <strong>Hangfire</strong>.
</p>

## Mục lục

- [Giới thiệu](#giới-thiệu)
- [Tính năng chính](#tính-năng-chính)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt](#cài-đặt)
- [Cấu trúc dự án](#cấu-trúc-dự án)
- [Tài khoản mặc định](#tài-khoản-mặc-định)
- [Các module](#các-module)
- [Danh sách entity](#danh-sách-entity)
- [Đặc biệt](#đặc-biệt)

---

## Giới thiệu

**Dashboard-X** (tên gốc: TradingServiceDashboard) là một hệ thống dashboard doanh nghiệp được xây dựng bằng ASP.NET Core 8 MVC, hỗ trợ quản lý đa ngành: Bán hàng, Marketing, Kho vận, Tài chính, Nhân sự và Chăm sóc khách hàng.

Hệ thống sử dụng **SQL Server** làm cơ sở dữ liệu, **Entity Framework Core** cho ORM, **ASP.NET Identity** cho xác thực phân quyền, **SignalR** cho thông báo thời gian thực, và **Hangfire** cho các job nền tự động.

---

## Tính năng chính

- **7 module phân quyền theo vai trò** — Executive, Sales, Marketing, Inventory, Finance, HR, Customer Service
- **Dashboard tổng quan** cho từng module với biểu đồ ApexCharts
- **50+ entity CRUD** — quản lý qua generic `CrudService<T>` registry
- **Thông báo thời gian thực** qua SignalR hub
- **Job nền tự động** qua Hangfire (tổng hợp dữ liệu, thông báo, reminder)
- **Import/Export Excel** cho tất cả bảng dữ liệu
- **Identity** — xác thực, phân quyền, khóa tài khoản, quản lý user
- **Auto-seed** — dữ liệu mẫu và tài khoản được tạo tự động khi khởi chạy

---

## Yêu cầu hệ thống

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (LocalDB hoặc Express)
- Trình duyệt hiện đại (Chrome, Edge, Firefox)

---

## Cài đặt

### 1. Clone và mở project

```bash
cd dashboard-x/dashboard-x
```

### 2. Cấu hình chuỗi kết nối

Sửa `appsettings.json` hoặc `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=TradingServiceDashboard;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### 3. Chạy ứng dụng

```bash
dotnet run
```

- Hệ thống sẽ tự động tạo database và seed dữ liệu mẫu + tài khoản.
- Hangfire dashboard: `http://localhost:<port>/hangfire`
- SignalR hub: `/notificationHub`

### 4. Truy cập

Mở trình duyệt tại `http://localhost:<port>` và đăng nhập bằng tài khoản bên dưới.

---

## Cấu trúc dự án

```
dashboard-x/
├── Controllers/            # 23 MVC controllers
├── Models/
│   ├── *.cs                # 64 entity models
│   ├── ViewModels/         # View-specific models
│   └── SD.cs               # Role & URL constants
├── Views/
│   ├── Shared/             # Layout, Partial, Navbar, Footer
│   ├── Auth/                # Login, Register, Forgot Password
│   ├── Executive/          # Dashboard + CRUD
│   ├── Sales/              # Dashboard + CRUD
│   ├── Marketing/          # Dashboard + CRUD
│   ├── Inventory/          # Dashboard + CRUD
│   ├── Finance/            # Dashboard + CRUD
│   ├── HumanResources/      # Dashboard + CRUD
│   └── CustomerService/    # Dashboard + CRUD
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbSeeder.cs         # 45+ seed methods
│   └── RoleSeeder.cs       # Roles & user accounts
├── Services/
│   ├── Dashboard/          # Dashboard data services (7 modules)
│   ├── Crud/               # Generic CRUD service registry
│   └── ExcelCrudService.cs  # Excel import/export
├── Jobs/
│   └── NotificationJobs.cs  # Hangfire background jobs
├── Hubs/
│   └── NotificationHub.cs   # SignalR real-time hub
└── wwwroot/
    ├── css/
    ├── js/
    └── vendor/             # Bootstrap, ApexCharts, libs
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
| 34 | Expense | Finance |
| 35 | ExpenseCategory | Finance |
| 36 | Employee | HR |
| 37 | Position | HR |
| 38 | Payroll | HR |
| 39 | LeaveRequest | HR |
| 40 | PerformanceReview | HR |
| 41 | JobOpening | HR |
| 42 | Applicant | HR |
| 43 | Attendance | HR |
| 44 | SupportTicket | Customer Service |
| 45 | DimDate | BI/Dashboard |
| 46 | KpiTarget | BI/Dashboard |

---

## Đặc biệt

- **RoleSeeder** — Tạo 7 vai trò + 7 tài khoản mặc định tự động khi khởi chạy.
- **DbSeeder** — Seed 45+ bảng dữ liệu mẫu (regions, branches, employees, customers, products, orders, invoices, marketing, HR...).
- **Generic CRUD** — `CrudServiceRegistry` quản lý CRUD cho 50+ entity types qua 7 controller.
- **Excel Import/Export** — Mỗi module hỗ trợ tải lên và tải xuống file Excel.
- **Thông báo thời gian thực** — SignalR hub gửi notification trực tiếp đến trình duyệt.
- **Background Jobs** — Hangfire chạy tổng hợp dữ liệu định kỳ cho từng module.
- **Authorization Policies** — 8 policy riêng biệt giới hạn quyền truy cập theo vai trò.

---

*Dashboard-X — Enterprise Operations Hub*
