using System.Text;
using Dashboard.Data;
using Dashboard.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Dashboard.Services;

public class DatabaseSchemaService : IDatabaseSchemaService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DatabaseSchemaService> _logger;
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, List<string>> DepartmentTableMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sales"] = new() { "Customers", "SalesOrders", "SalesOrderDetails", "SalesInvoices", "SalesInvoiceDetails",
                             "Opportunities", "OpportunityStages", "OpportunityStageHistories",
                             "Quotes", "SalesChannels", "Employees", "Branches", "Regions", "CustomerGroups" },
        ["finance"] = new() { "Expenses", "ExpenseCategories", "CustomerPayments", "SupplierPayments",
                              "SalesInvoices", "PurchaseInvoices", "Employees", "Branches" },
        ["marketing"] = new() { "MarketingCampaigns", "MarketingLeads", "MarketingSpendDaily",
                               "SalesChannels", "Employees", "Customers", "Regions" },
        ["inventory"] = new() { "Products", "ProductCategories", "Inventories", "Warehouses",
                                "StockTransactions", "InventorySnapshots",
                                "Suppliers", "PurchaseOrders", "PurchaseOrderDetails", "PurchaseReceipts",
                                "Branches", "Employees" },
        ["hr"] = new() { "Employees", "Departments", "Positions", "Branches", "Regions",
                         "LeaveRequests", "Payrolls", "JobOpenings", "Applicants",
                         "Attendances", "PerformanceReviews" },
        ["cskh"] = new() { "Customers", "SupportTickets", "SalesOrders", "SalesOrderDetails",
                           "Employees", "Branches", "CustomerGroups" },
        ["executive"] = new() { "Customers", "SalesOrders", "SalesOrderDetails", "SalesInvoices",
                                "SalesInvoiceDetails", "SalesReturns", "SalesReturnDetails",
                                "Products", "ProductCategories", "Inventories", "Warehouses",
                                "StockTransactions", "Opportunities", "OpportunityStages",
                                "Quotes", "SalesChannels", "MarketingCampaigns", "MarketingLeads",
                                "MarketingSpendDaily", "Expenses", "ExpenseCategories",
                                "CustomerPayments", "SupplierPayments",
                                "PurchaseOrders", "PurchaseOrderDetails", "PurchaseReceipts",
                                "PurchaseInvoices", "PurchaseInvoiceDetails",
                                "Employees", "Departments", "Positions", "Branches", "Regions",
                                "LeaveRequests", "Payrolls", "JobOpenings", "Applicants",
                                "Attendances", "PerformanceReviews", "SupportTickets",
                                "Suppliers", "CustomerGroups", "KpiTargets" }
    };

    private static readonly Dictionary<string, string> TableDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers"] = "Danh sách khách hàng, thông tin công ty, địa chỉ, giới hạn tín dụng",
        ["SalesOrders"] = "Đơn hàng bán với ngày đặt, tổng tiền, trạng thái thanh toán/vận chuyển",
        ["SalesOrderDetails"] = "Chi tiết từng dòng sản phẩm trong đơn hàng (sản phẩm, số lượng, đơn giá)",
        ["SalesInvoices"] = "Hóa đơn bán hàng với ngày, tổng tiền, số tiền đã trả, công nợ",
        ["SalesInvoiceDetails"] = "Chi tiết dòng sản phẩm trong hóa đơn bán hàng",
        ["SalesReturns"] = "Phiếu trả hàng với ngày trả, số tiền hoàn",
        ["SalesReturnDetails"] = "Chi tiết sản phẩm trong phiếu trả hàng",
        ["Opportunities"] = "Cơ hội kinh doanh với giai đoạn, giá trị kỳ vọng, xác suất thành công, trạng thái (Open/Won/Lost)",
        ["OpportunityStages"] = "Các giai đoạn của pipeline cơ hội (vd: Khám phá, Đề xuất, Đàm phán)",
        ["OpportunityStageHistories"] = "Lịch sử thay đổi giai đoạn của từng cơ hội",
        ["Quotes"] = "Báo giá gửi khách hàng với ngày, tổng tiền, trạng thái",
        ["SalesChannels"] = "Kênh bán hàng (Online, Offline, Đại lý, v.v.)",
        ["Expenses"] = "Phiếu chi với ngày, số tiền, loại chi phí, trạng thái duyệt",
        ["ExpenseCategories"] = "Danh mục chi phí (vd: Văn phòng phẩm, Công tác phí, Marketing)",
        ["CustomerPayments"] = "Phiếu thu từ khách hàng với ngày, số tiền, phương thức thanh toán",
        ["SupplierPayments"] = "Phiếu chi cho nhà cung cấp với ngày, số tiền, phương thức",
        ["PurchaseInvoices"] = "Hóa đơn mua hàng từ nhà cung cấp",
        ["MarketingCampaigns"] = "Chiến dịch marketing với ngày, ngân sách, chi tiêu thực tế, trạng thái",
        ["MarketingLeads"] = "Leads marketing với nguồn, điểm số, trạng thái chuyển đổi, ngày chuyển đổi",
        ["MarketingSpendDaily"] = "Chi tiêu marketing hàng ngày theo kênh (Impressions, Clicks, Conversions, CPM, CPC, CPA)",
        ["Products"] = "Danh sách sản phẩm với tên, mã, giá bán, giá vốn, đơn vị tính, điểm đặt hàng lại",
        ["ProductCategories"] = "Danh mục sản phẩm phân cấp cha-con",
        ["Inventories"] = "Tồn kho hiện tại theo sản phẩm và kho (số lượng tồn, số lượng đặt trước, số lượng khả dụng)",
        ["Warehouses"] = "Danh sách kho hàng với địa chỉ, quản lý kho",
        ["StockTransactions"] = "Giao dịch nhập/xuất kho với loại giao dịch, số lượng, số dư",
        ["InventorySnapshots"] = "Ảnh chụp tồn kho định kỳ",
        ["Suppliers"] = "Danh sách nhà cung cấp với thông tin liên hệ, điều khoản thanh toán",
        ["PurchaseOrders"] = "Đơn mua hàng với ngày, tổng tiền, trạng thái giao hàng/thanh toán",
        ["PurchaseOrderDetails"] = "Chi tiết dòng sản phẩm trong đơn mua hàng",
        ["PurchaseReceipts"] = "Phiếu nhập kho với ngày, số tiền",
        ["PurchaseReceiptDetails"] = "Chi tiết dòng sản phẩm trong phiếu nhập kho",
        ["PurchaseInvoiceDetails"] = "Chi tiết dòng sản phẩm trong hóa đơn mua hàng",
        ["Employees"] = "Nhân viên với mã, họ tên, phòng ban, chức vụ, lương cơ bản, ngày vào làm",
        ["Departments"] = "Phòng ban với mã và tên",
        ["Positions"] = "Chức vụ với mã, tên, cấp bậc",
        ["Branches"] = "Chi nhánh với mã, tên, địa chỉ, có phải trụ sở chính không",
        ["Regions"] = "Vùng/khu vực địa lý",
        ["LeaveRequests"] = "Đơn xin nghỉ với loại nghỉ, ngày bắt đầu, ngày kết thúc, số ngày, trạng thái duyệt",
        ["Payrolls"] = "Bảng lương với lương cơ bản, làm thêm giờ, thưởng, khấu trừ, thuế, lương thực nhận",
        ["JobOpenings"] = "Vị trí tuyển dụng với mức lương, số lượng tuyển, trạng thái",
        ["Applicants"] = "Ứng viên ứng tuyển với thông tin liên hệ, trạng thái",
        ["Attendances"] = "Chấm công với giờ vào, giờ ra, tổng giờ làm",
        ["PerformanceReviews"] = "Đánh giá hiệu suất nhân viên với điểm tổng quan",
        ["SupportTickets"] = "Ticket hỗ trợ khách hàng với loại, mức ưu tiên, trạng thái, CSAT score",
        ["CustomerGroups"] = "Nhóm khách hàng",
        ["KpiTargets"] = "Mục tiêu KPI với giá trị mục tiêu, giá trị thực tế, % hoàn thành",
        ["DimDates"] = "Bảng chiều ngày cho báo cáo (DateKey, Year, Month, Quarter, Week)",
        ["Quotes"] = "Báo giá"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> ColumnDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers"] = new() {
            ["CustomerID"] = "ID khách hàng (PK)",
            ["CustomerCode"] = "Mã khách hàng",
            ["CustomerName"] = "Tên khách hàng / công ty",
            ["CustomerType"] = "Loại khách hàng (VIP, Regular, v.v.)",
            ["Phone"] = "Số điện thoại",
            ["Email"] = "Email",
            ["AddressLine"] = "Địa chỉ",
            ["City"] = "Thành phố",
            ["Province"] = "Tỉnh",
            ["CreditLimit"] = "Hạn mức tín dụng (VNĐ)",
            ["PaymentTermDays"] = "Số ngày được nợ",
            ["RegionID"] = "ID vùng (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["CustomerGroupID"] = "ID nhóm khách hàng (FK)",
            ["CreatedAt"] = "Ngày tạo"
        },
        ["SalesOrders"] = new() {
            ["SalesOrderID"] = "ID đơn hàng (PK)",
            ["OrderNumber"] = "Số đơn hàng",
            ["CustomerID"] = "ID khách hàng (FK)",
            ["OrderDate"] = "Ngày đặt hàng",
            ["DeliveryDate"] = "Ngày giao hàng dự kiến",
            ["SubTotal"] = "Tổng phụ (chưa thuế)",
            ["TaxAmount"] = "Thuế",
            ["DiscountAmount"] = "Giảm giá",
            ["TotalAmount"] = "Tổng cộng (VNĐ)",
            ["PaidAmount"] = "Đã thanh toán (VNĐ)",
            ["OutstandingAmount"] = "Còn nợ (VNĐ)",
            ["PaymentStatus"] = "Trạng thái thanh toán (Unpaid/Partial/Paid)",
            ["DeliveryStatus"] = "Trạng thái giao hàng (Pending/Shipped/Delivered)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["SalesChannelID"] = "ID kênh bán hàng (FK)",
            ["SalesEmployeeID"] = "ID nhân viên bán hàng (FK)",
            ["CreatedAt"] = "Ngày tạo"
        },
        ["SalesOrderDetails"] = new() {
            ["SalesOrderDetailID"] = "ID chi tiết đơn hàng (PK)",
            ["SalesOrderID"] = "ID đơn hàng (FK)",
            ["ProductID"] = "ID sản phẩm (FK)",
            ["Quantity"] = "Số lượng",
            ["UnitPrice"] = "Đơn giá (VNĐ)",
            ["DiscountPercent"] = "% giảm giá",
            ["TaxPercent"] = "% thuế",
            ["LineTotal"] = "Thành tiền (VNĐ)"
        },
        ["SalesInvoices"] = new() {
            ["InvoiceID"] = "ID hóa đơn (PK)",
            ["InvoiceNumber"] = "Số hóa đơn",
            ["SalesOrderID"] = "ID đơn hàng (FK)",
            ["CustomerID"] = "ID khách hàng (FK)",
            ["InvoiceDate"] = "Ngày hóa đơn",
            ["DueDate"] = "Ngày đến hạn thanh toán",
            ["SubTotal"] = "Tổng phụ (VNĐ)",
            ["TaxAmount"] = "Thuế (VNĐ)",
            ["TotalAmount"] = "Tổng cộng (VNĐ)",
            ["PaidAmount"] = "Đã trả (VNĐ)",
            ["OutstandingAmount"] = "Còn nợ (VNĐ)",
            ["PaymentStatus"] = "Trạng thái (Unpaid/Partial/Paid)",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["Products"] = new() {
            ["ProductID"] = "ID sản phẩm (PK)",
            ["ProductCode"] = "Mã sản phẩm",
            ["ProductName"] = "Tên sản phẩm",
            ["CategoryID"] = "ID danh mục (FK)",
            ["UnitOfMeasure"] = "Đơn vị tính",
            ["SalePrice"] = "Giá bán (VNĐ)",
            ["CostPrice"] = "Giá vốn (VNĐ)",
            ["ReorderLevel"] = "Điểm đặt hàng lại",
            ["MaxStockLevel"] = "Tồn kho tối đa",
            ["IsStockItem"] = "Có phải hàng tồn kho không (bool)",
            ["IsActive"] = "Còn kinh doanh không (bool)",
            ["Brand"] = "Thương hiệu",
            ["CreatedAt"] = "Ngày tạo"
        },
        ["Inventories"] = new() {
            ["InventoryID"] = "ID tồn kho (PK)",
            ["ProductID"] = "ID sản phẩm (FK)",
            ["WarehouseID"] = "ID kho (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["QuantityOnHand"] = "Số lượng tồn",
            ["QuantityReserved"] = "Số lượng đặt trước",
            ["QuantityAvailable"] = "Số lượng khả dụng",
            ["ReorderPoint"] = "Điểm đặt hàng lại",
            ["ReorderQuantity"] = "Số lượng đặt hàng lại",
            ["AverageCost"] = "Giá vốn trung bình (VNĐ)",
            ["LastUpdatedAt"] = "Cập nhật lần cuối"
        },
        ["Employees"] = new() {
            ["EmployeeID"] = "ID nhân viên (PK)",
            ["EmployeeCode"] = "Mã nhân viên",
            ["FullName"] = "Họ tên",
            ["Gender"] = "Giới tính",
            ["Phone"] = "Điện thoại",
            ["Email"] = "Email",
            ["DepartmentID"] = "ID phòng ban (FK)",
            ["PositionID"] = "ID chức vụ (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["ManagerID"] = "ID quản lý (FK, tự tham chiếu)",
            ["BaseSalary"] = "Lương cơ bản (VNĐ)",
            ["HireDate"] = "Ngày vào làm",
            ["TerminationDate"] = "Ngày nghỉ việc",
            ["EmploymentType"] = "Loại hình việc làm (FullTime/PartTime/Contract)",
            ["IsActive"] = "Còn làm việc không (bool)",
            ["CreatedAt"] = "Ngày tạo"
        },
        ["Departments"] = new() {
            ["DepartmentID"] = "ID phòng ban (PK)",
            ["DepartmentCode"] = "Mã phòng ban",
            ["DepartmentName"] = "Tên phòng ban",
            ["IsActive"] = "Còn hoạt động không (bool)"
        },
        ["Positions"] = new() {
            ["PositionID"] = "ID chức vụ (PK)",
            ["PositionCode"] = "Mã chức vụ",
            ["PositionName"] = "Tên chức vụ",
            ["PositionLevel"] = "Cấp bậc (Junior/Middle/Senior/Manager/Director)"
        },
        ["Opportunities"] = new() {
            ["OpportunityID"] = "ID cơ hội (PK)",
            ["OpportunityCode"] = "Mã cơ hội",
            ["CustomerID"] = "ID khách hàng (FK)",
            ["StageID"] = "ID giai đoạn (FK)",
            ["OwnerEmployeeID"] = "ID nhân viên phụ trách (FK)",
            ["OpportunityName"] = "Tên cơ hội",
            ["ExpectedCloseDate"] = "Ngày đóng dự kiến",
            ["EstimatedValue"] = "Giá trị kỳ vọng (VNĐ)",
            ["Probability"] = "Xác suất thành công (%)",
            ["Status"] = "Trạng thái (Open/Won/Lost)",
            ["WonReason"] = "Lý do thắng",
            ["LostReason"] = "Lý do thua",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["CreatedAt"] = "Ngày tạo"
        },
        ["OpportunityStages"] = new() {
            ["StageID"] = "ID giai đoạn (PK)",
            ["StageName"] = "Tên giai đoạn (vd: Khám phá, Định giá, Đàm phán)",
            ["StageOrder"] = "Thứ tự giai đoạn",
            ["IsClosedStage"] = "Có phải giai đoạn đóng không (bool)",
            ["IsWonStage"] = "Có phải giai đoạn thắng không (bool)",
            ["IsLostStage"] = "Có phải giai đoạn thua không (bool)"
        },
        ["MarketingCampaigns"] = new() {
            ["CampaignID"] = "ID chiến dịch (PK)",
            ["CampaignName"] = "Tên chiến dịch",
            ["StartDate"] = "Ngày bắt đầu",
            ["EndDate"] = "Ngày kết thúc",
            ["Budget"] = "Ngân sách (VNĐ)",
            ["ActualSpend"] = "Chi tiêu thực tế (VNĐ)",
            ["Status"] = "Trạng thái (Planning/Active/Completed/Cancelled)",
            ["Channel"] = "Kênh (Social/Email/SEO/Ads/Event)",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["MarketingLeads"] = new() {
            ["LeadID"] = "ID lead (PK)",
            ["CampaignID"] = "ID chiến dịch (FK)",
            ["LeadName"] = "Tên người quan tâm",
            ["CompanyName"] = "Tên công ty",
            ["Phone"] = "Điện thoại",
            ["Email"] = "Email",
            ["Source"] = "Nguồn (Website/Referral/Event/Ad)",
            ["Status"] = "Trạng thái (New/Contacted/Qualified/Converted/Lost)",
            ["LeadScore"] = "Điểm lead (0-100)",
            ["MQLDate"] = "Ngày đạt MQL",
            ["SQLDate"] = "Ngày đạt SQL",
            ["ConvertedDate"] = "Ngày chuyển đổi thành khách hàng",
            ["ConvertedCustomerID"] = "ID khách hàng chuyển đổi (FK)",
            ["AssignedEmployeeID"] = "ID nhân viên phụ trách (FK)"
        },
        ["MarketingSpendDaily"] = new() {
            ["ID"] = "ID (PK)",
            ["CampaignID"] = "ID chiến dịch (FK)",
            ["SpendDate"] = "Ngày chi tiêu",
            ["Channel"] = "Kênh (Facebook/Google/Zalo/Website)",
            ["Impressions"] = "Số lần hiển thị",
            ["Clicks"] = "Số lần nhấp",
            ["Conversions"] = "Số chuyển đổi",
            ["CPM"] = "Chi phí trên 1000 lần hiển thị (VNĐ)",
            ["CPC"] = "Chi phí trên mỗi nhấp chuột (VNĐ)",
            ["CPA"] = "Chi phí trên mỗi chuyển đổi (VNĐ)",
            ["SpendAmount"] = "Số tiền chi (VNĐ)"
        },
        ["Expenses"] = new() {
            ["ExpenseID"] = "ID chi phí (PK)",
            ["ExpenseNumber"] = "Số phiếu chi",
            ["EmployeeID"] = "ID nhân viên yêu cầu (FK)",
            ["CategoryID"] = "ID loại chi phí (FK)",
            ["ExpenseDate"] = "Ngày chi",
            ["Amount"] = "Số tiền (VNĐ)",
            ["Description"] = "Mô tả",
            ["Status"] = "Trạng thái (Pending/Approved/Rejected)",
            ["ApprovedByEmployeeID"] = "ID người duyệt (FK)",
            ["ApprovedDate"] = "Ngày duyệt",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["ExpenseCategories"] = new() {
            ["CategoryID"] = "ID loại chi phí (PK)",
            ["CategoryCode"] = "Mã loại chi phí",
            ["CategoryName"] = "Tên loại chi phí",
            ["IsActive"] = "Còn hoạt động không (bool)"
        },
        ["CustomerPayments"] = new() {
            ["PaymentID"] = "ID phiếu thu (PK)",
            ["PaymentNumber"] = "Số phiếu thu",
            ["CustomerID"] = "ID khách hàng (FK)",
            ["SalesInvoiceID"] = "ID hóa đơn bán (FK, nullable)",
            ["PaymentDate"] = "Ngày thu",
            ["Amount"] = "Số tiền (VNĐ)",
            ["PaymentMethod"] = "Phương thức (Cash/Transfer/Card)",
            ["ReferenceNumber"] = "Số tham chiếu (số tài khoản, mã giao dịch)",
            ["Notes"] = "Ghi chú",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["SupplierPayments"] = new() {
            ["PaymentID"] = "ID phiếu chi (PK)",
            ["PaymentNumber"] = "Số phiếu chi",
            ["SupplierID"] = "ID nhà cung cấp (FK)",
            ["PurchaseInvoiceID"] = "ID hóa đơn mua (FK, nullable)",
            ["PaymentDate"] = "Ngày chi",
            ["Amount"] = "Số tiền (VNĐ)",
            ["PaymentMethod"] = "Phương thức (Cash/Transfer/Card)",
            ["ReferenceNumber"] = "Số tham chiếu",
            ["Notes"] = "Ghi chú",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["Warehouses"] = new() {
            ["WarehouseID"] = "ID kho (PK)",
            ["WarehouseCode"] = "Mã kho",
            ["WarehouseName"] = "Tên kho",
            ["Address"] = "Địa chỉ kho",
            ["IsActive"] = "Còn hoạt động không (bool)",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["StockTransactions"] = new() {
            ["TransactionID"] = "ID giao dịch kho (PK)",
            ["TransactionNumber"] = "Số giao dịch",
            ["ProductID"] = "ID sản phẩm (FK)",
            ["WarehouseID"] = "ID kho (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["TransactionType"] = "Loại (Receipt/Issue/Adjustment/Transfer)",
            ["Quantity"] = "Số lượng",
            ["QuantityBefore"] = "Số lượng trước giao dịch",
            ["QuantityAfter"] = "Số lượng sau giao dịch",
            ["TransactionDate"] = "Ngày giao dịch",
            ["ReferenceType"] = "Loại tham chiếu (PurchaseOrder/SalesOrder/Adjustment)",
            ["ReferenceID"] = "ID tham chiếu",
            ["Notes"] = "Ghi chú"
        },
        ["LeaveRequests"] = new() {
            ["LeaveRequestID"] = "ID đơn nghỉ (PK)",
            ["EmployeeID"] = "ID nhân viên (FK)",
            ["LeaveType"] = "Loại nghỉ (Annual/Sick/Unpaid/Maternity/Paternity)",
            ["StartDate"] = "Ngày bắt đầu nghỉ",
            ["EndDate"] = "Ngày kết thúc nghỉ",
            ["TotalDays"] = "Tổng số ngày nghỉ",
            ["Reason"] = "Lý do",
            ["Status"] = "Trạng thái (Pending/Approved/Rejected)",
            ["ApprovedByEmployeeID"] = "ID người duyệt (FK)",
            ["ApprovedDate"] = "Ngày duyệt"
        },
        ["Payrolls"] = new() {
            ["PayrollID"] = "ID bảng lương (PK)",
            ["EmployeeID"] = "ID nhân viên (FK)",
            ["PayrollMonth"] = "Tháng tính lương (yyyy-MM)",
            ["BaseSalary"] = "Lương cơ bản (VNĐ)",
            ["OvertimeHours"] = "Số giờ làm thêm",
            ["OvertimeRate"] = "Đơn giá làm thêm (VNĐ/giờ)",
            ["OvertimeAmount"] = "Tiền làm thêm (VNĐ)",
            ["BonusAmount"] = "Thưởng (VNĐ)",
            ["DeductionAmount"] = "Khấu trừ (VNĐ)",
            ["TaxAmount"] = "Thuế TNCN (VNĐ)",
            ["NetSalary"] = "Lương thực nhận (VNĐ)",
            ["PaymentDate"] = "Ngày thanh toán",
            ["Status"] = "Trạng thái (Pending/Paid)"
        },
        ["JobOpenings"] = new() {
            ["JobOpeningID"] = "ID vị trí tuyển dụng (PK)",
            ["JobTitle"] = "Chức danh tuyển dụng",
            ["DepartmentID"] = "ID phòng ban (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["EmploymentType"] = "Loại hình (FullTime/PartTime/Contract/Intern)",
            ["SalaryMin"] = "Lương tối thiểu (VNĐ)",
            ["SalaryMax"] = "Lương tối đa (VNĐ)",
            ["NumberOfPositions"] = "Số lượng tuyển",
            ["Status"] = "Trạng thái (Open/Closed/Filled)",
            ["PostedDate"] = "Ngày đăng tuyển",
            ["ClosedDate"] = "Ngày đóng tuyển",
            ["Description"] = "Mô tả công việc"
        },
        ["Applicants"] = new() {
            ["ApplicantID"] = "ID ứng viên (PK)",
            ["JobOpeningID"] = "ID vị trí tuyển dụng (FK)",
            ["FullName"] = "Họ tên ứng viên",
            ["Email"] = "Email",
            ["Phone"] = "Điện thoại",
            ["Status"] = "Trạng thái (Applied/Screening/Interview/Offer/Hired/Rejected)",
            ["AppliedDate"] = "Ngày ứng tuyển"
        },
        ["Attendances"] = new() {
            ["AttendanceID"] = "ID chấm công (PK)",
            ["EmployeeID"] = "ID nhân viên (FK)",
            ["AttendanceDate"] = "Ngày chấm công",
            ["CheckInTime"] = "Giờ vào",
            ["CheckOutTime"] = "Giờ ra",
            ["TotalHours"] = "Tổng giờ làm",
            ["Status"] = "Trạng thái (Present/Absent/Late/EarlyLeave)",
            ["Notes"] = "Ghi chú"
        },
        ["PerformanceReviews"] = new() {
            ["ReviewID"] = "ID đánh giá (PK)",
            ["EmployeeID"] = "ID nhân viên (FK)",
            ["ReviewPeriod"] = "Kỳ đánh giá (vd: 2024-Q1)",
            ["ReviewDate"] = "Ngày đánh giá",
            ["OverallRating"] = "Điểm tổng quan (1-5)",
            ["Comments"] = "Nhận xét"
        },
        ["SupportTickets"] = new() {
            ["TicketID"] = "ID ticket (PK)",
            ["TicketNumber"] = "Số ticket",
            ["CustomerID"] = "ID khách hàng (FK)",
            ["SalesOrderID"] = "ID đơn hàng (FK, nullable)",
            ["AssignedEmployeeID"] = "ID nhân viên phụ trách (FK)",
            ["TicketType"] = "Loại (Bug/Feature/Complaint/Consultation)",
            ["Priority"] = "Mức ưu tiên (Low/Medium/High/Critical)",
            ["Subject"] = "Chủ đề",
            ["Description"] = "Mô tả",
            ["Status"] = "Trạng thái (Open/InProgress/Resolved/Closed)",
            ["CsatScore"] = "Điểm hài lòng (1-5, nullable)",
            ["FirstResponseAt"] = "Thời gian phản hồi đầu tiên",
            ["ResolvedAt"] = "Thời gian giải quyết",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["CreatedAt"] = "Ngày tạo"
        },
        ["PurchaseOrders"] = new() {
            ["PurchaseOrderID"] = "ID đơn mua (PK)",
            ["OrderNumber"] = "Số đơn mua",
            ["SupplierID"] = "ID nhà cung cấp (FK)",
            ["WarehouseID"] = "ID kho nhận (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["OrderDate"] = "Ngày đặt",
            ["ExpectedDeliveryDate"] = "Ngày giao dự kiến",
            ["ActualDeliveryDate"] = "Ngày giao thực tế",
            ["SubTotal"] = "Tổng phụ (VNĐ)",
            ["TaxAmount"] = "Thuế (VNĐ)",
            ["TotalAmount"] = "Tổng cộng (VNĐ)",
            ["DeliveryStatus"] = "Trạng thái giao hàng (Pending/Partial/Received)",
            ["PaymentStatus"] = "Trạng thái thanh toán (Unpaid/Partial/Paid)",
            ["Status"] = "Trạng thái đơn (Open/Closed/Cancelled)",
            ["RequestedByEmployeeID"] = "ID người yêu cầu (FK)",
            ["ApprovedByEmployeeID"] = "ID người duyệt (FK)"
        },
        ["PurchaseOrderDetails"] = new() {
            ["PurchaseOrderDetailID"] = "ID chi tiết (PK)",
            ["PurchaseOrderID"] = "ID đơn mua (FK)",
            ["ProductID"] = "ID sản phẩm (FK)",
            ["Quantity"] = "Số lượng đặt",
            ["ReceivedQuantity"] = "Số lượng đã nhận",
            ["UnitPrice"] = "Đơn giá (VNĐ)",
            ["LineTotal"] = "Thành tiền (VNĐ)"
        },
        ["PurchaseInvoices"] = new() {
            ["PurchaseInvoiceID"] = "ID hóa đơn mua (PK)",
            ["InvoiceNumber"] = "Số hóa đơn",
            ["PurchaseOrderID"] = "ID đơn mua (FK)",
            ["SupplierID"] = "ID nhà cung cấp (FK)",
            ["InvoiceDate"] = "Ngày hóa đơn",
            ["DueDate"] = "Ngày đến hạn",
            ["SubTotal"] = "Tổng phụ (VNĐ)",
            ["TaxAmount"] = "Thuế (VNĐ)",
            ["TotalAmount"] = "Tổng cộng (VNĐ)",
            ["PaidAmount"] = "Đã trả (VNĐ)",
            ["OutstandingAmount"] = "Còn nợ (VNĐ)",
            ["PaymentStatus"] = "Trạng thái (Unpaid/Partial/Paid)",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["PurchaseReceipts"] = new() {
            ["PurchaseReceiptID"] = "ID phiếu nhập (PK)",
            ["ReceiptNumber"] = "Số phiếu nhập",
            ["PurchaseOrderID"] = "ID đơn mua (FK)",
            ["WarehouseID"] = "ID kho nhận (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["ReceiptDate"] = "Ngày nhập",
            ["TotalAmount"] = "Tổng tiền (VNĐ)",
            ["Notes"] = "Ghi chú"
        },
        ["PurchaseReceiptDetails"] = new() {
            ["PurchaseReceiptDetailID"] = "ID chi tiết (PK)",
            ["PurchaseReceiptID"] = "ID phiếu nhập (FK)",
            ["ProductID"] = "ID sản phẩm (FK)",
            ["Quantity"] = "Số lượng nhập",
            ["UnitPrice"] = "Đơn giá (VNĐ)",
            ["LineTotal"] = "Thành tiền (VNĐ)"
        },
        ["Suppliers"] = new() {
            ["SupplierID"] = "ID nhà cung cấp (PK)",
            ["SupplierCode"] = "Mã nhà cung cấp",
            ["SupplierName"] = "Tên nhà cung cấp",
            ["ContactName"] = "Người liên hệ",
            ["Phone"] = "Điện thoại",
            ["Email"] = "Email",
            ["AddressLine"] = "Địa chỉ",
            ["City"] = "Thành phố",
            ["PaymentTermDays"] = "Số ngày được nợ",
            ["IsActive"] = "Còn hoạt động không (bool)"
        },
        ["Branches"] = new() {
            ["BranchID"] = "ID chi nhánh (PK)",
            ["BranchCode"] = "Mã chi nhánh",
            ["BranchName"] = "Tên chi nhánh",
            ["Address"] = "Địa chỉ",
            ["Phone"] = "Điện thoại",
            ["IsHeadOffice"] = "Có phải trụ sở chính không (bool)",
            ["IsActive"] = "Còn hoạt động không (bool)",
            ["RegionID"] = "ID vùng (FK)"
        },
        ["Regions"] = new() {
            ["RegionID"] = "ID vùng (PK)",
            ["RegionCode"] = "Mã vùng",
            ["RegionName"] = "Tên vùng"
        },
        ["CustomerGroups"] = new() {
            ["CustomerGroupID"] = "ID nhóm khách hàng (PK)",
            ["GroupCode"] = "Mã nhóm",
            ["GroupName"] = "Tên nhóm",
            ["Description"] = "Mô tả"
        },
        ["ProductCategories"] = new() {
            ["CategoryID"] = "ID danh mục (PK)",
            ["CategoryCode"] = "Mã danh mục",
            ["CategoryName"] = "Tên danh mục",
            ["ParentCategoryID"] = "ID danh mục cha (FK, nullable, tự tham chiếu)",
            ["IsActive"] = "Còn hoạt động không (bool)"
        },
        ["KpiTargets"] = new() {
            ["TargetID"] = "ID mục tiêu (PK)",
            ["TargetName"] = "Tên mục tiêu",
            ["DepartmentID"] = "ID phòng ban (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["TargetValue"] = "Giá trị mục tiêu",
            ["ActualValue"] = "Giá trị thực tế",
            ["AchievementPercent"] = "% hoàn thành",
            ["StartDate"] = "Ngày bắt đầu",
            ["EndDate"] = "Ngày kết thúc",
            ["Status"] = "Trạng thái (OnTrack/AtRisk/Behind/Achieved)"
        },
        ["Quotes"] = new() {
            ["QuoteID"] = "ID báo giá (PK)",
            ["QuoteNumber"] = "Số báo giá",
            ["CustomerID"] = "ID khách hàng (FK)",
            ["QuoteDate"] = "Ngày báo giá",
            ["ValidUntil"] = "Hiệu lực đến ngày",
            ["SubTotal"] = "Tổng phụ (VNĐ)",
            ["TaxAmount"] = "Thuế (VNĐ)",
            ["TotalAmount"] = "Tổng cộng (VNĐ)",
            ["Status"] = "Trạng thái (Draft/Sent/Accepted/Declined/Expired)",
            ["BranchID"] = "ID chi nhánh (FK)"
        },
        ["SalesChannels"] = new() {
            ["ChannelID"] = "ID kênh (PK)",
            ["ChannelCode"] = "Mã kênh",
            ["ChannelName"] = "Tên kênh (vd: Online, Offline, Đại lý)",
            ["IsActive"] = "Còn hoạt động không (bool)"
        },
        ["InventorySnapshots"] = new() {
            ["SnapshotID"] = "ID ảnh chụp (PK)",
            ["SnapshotDate"] = "Ngày chụp",
            ["ProductID"] = "ID sản phẩm (FK)",
            ["WarehouseID"] = "ID kho (FK)",
            ["BranchID"] = "ID chi nhánh (FK)",
            ["QuantityOnHand"] = "Số lượng tồn",
            ["AverageCost"] = "Giá vốn trung bình (VNĐ)"
        },
        ["OpportunityStageHistories"] = new() {
            ["HistoryID"] = "ID lịch sử (PK)",
            ["OpportunityID"] = "ID cơ hội (FK)",
            ["FromStageID"] = "ID giai đoạn cũ (FK, nullable)",
            ["ToStageID"] = "ID giai đoạn mới (FK)",
            ["ChangedAt"] = "Thời gian thay đổi",
            ["ChangedByEmployeeID"] = "ID nhân viên thay đổi (FK)"
        }
    };

    public DatabaseSchemaService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<DatabaseSchemaService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> GetSchemaPromptForDepartmentAsync(string department)
    {
        var cacheKey = $"schema_prompt_{department.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var schema = await GetSchemaForDepartmentAsync(department);
        var prompt = BuildSchemaPrompt(schema, department);

        _cache.Set(cacheKey, prompt, CacheDuration);
        return prompt;
    }

    public async Task<string> GetFullSchemaPromptAsync()
    {
        var cacheKey = "schema_prompt_full";

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var schema = await GetSchemaForDepartmentAsync("executive");
        var prompt = BuildSchemaPrompt(schema, "all");

        _cache.Set(cacheKey, prompt, CacheDuration);
        return prompt;
    }

    public async Task<List<TableSchemaInfo>> GetSchemaForDepartmentAsync(string department)
    {
        var cacheKey = $"schema_tables_{department.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out List<TableSchemaInfo>? cached) && cached != null)
            return cached;

        var tableNames = DepartmentTableMap.TryGetValue(department, out var tables)
            ? tables
            : DepartmentTableMap["executive"];

        var result = new List<TableSchemaInfo>();

        foreach (var tableName in tableNames)
        {
            var tableInfo = new TableSchemaInfo
            {
                TableName = tableName,
                FriendlyName = tableName,
                Description = TableDescriptions.GetValueOrDefault(tableName, $"Bảng {tableName}")
            };

            var columns = await GetTableColumnsAsync(tableName);
            tableInfo.Columns = columns;

            result.Add(tableInfo);
        }

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<Dictionary<string, List<string>>> GetTableRelationshipsAsync(string department)
    {
        var schema = await GetSchemaForDepartmentAsync(department);
        var relationships = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in schema)
        {
            var refs = new List<string>();
            foreach (var col in table.Columns.Where(c => c.IsForeignKey))
            {
                if (!string.IsNullOrEmpty(col.ReferencedTable))
                    refs.Add($"{col.ColumnName} -> {col.ReferencedTable}({col.ReferencedColumn})");
            }
            if (refs.Count > 0)
                relationships[table.TableName] = refs;
        }

        return relationships;
    }

    private async Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName)
    {
        var columns = new List<TableColumnInfo>();

        try
        {
            var schemaQuery = @"
                SELECT
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY,
                    fk.REFERENCED_TABLE_NAME,
                    fk.REFERENCED_COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                        AND ku.TABLE_NAME = @TableName
                ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
                LEFT JOIN (
                    SELECT
                        cu.COLUMN_NAME,
                        cu.TABLE_NAME,
                        kcu.TABLE_NAME AS REFERENCED_TABLE_NAME,
                        kcu.COLUMN_NAME AS REFERENCED_COLUMN_NAME
                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu
                        ON rc.CONSTRAINT_NAME = cu.CONSTRAINT_NAME
                    JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE kcu
                        ON rc.UNIQUE_CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    WHERE cu.TABLE_NAME = @TableName
                ) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
                WHERE c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION";

            var conn = _context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = schemaQuery;
            var param = cmd.CreateParameter();
            param.ParameterName = "@TableName";
            param.Value = tableName;
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var col = new TableColumnInfo
                {
                    TableName = tableName,
                    ColumnName = colName,
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES",
                    IsPrimaryKey = reader.GetInt32(4) == 1,
                    ReferencedTable = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ReferencedColumn = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
                col.IsForeignKey = !string.IsNullOrEmpty(col.ReferencedTable);

                if (ColumnDescriptions.TryGetValue(tableName, out var colDescs))
                    col.FriendlyDescription = colDescs.GetValueOrDefault(colName);
                else
                    col.FriendlyDescription = colName;

                columns.Add(col);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load columns for table {TableName}", tableName);
        }

        return columns;
    }

    private string BuildSchemaPrompt(List<TableSchemaInfo> schema, string department)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Database Schema");
        sb.AppendLine($"Department context: {department}");
        sb.AppendLine();
        sb.AppendLine("You are a SQL query generator. Given a user's natural language question, generate a valid T-SQL SELECT query.");
        sb.AppendLine();
        sb.AppendLine("### IMPORTANT RULES:");
        sb.AppendLine("1. ONLY generate SELECT queries - NEVER UPDATE, DELETE, INSERT, DROP, ALTER, CREATE, TRUNCATE, or any write operations");
        sb.AppendLine("2. ALWAYS use TOP to limit results (TOP 100 max)");
        sb.AppendLine("3. Use proper JOINs with explicit ON clauses");
        sb.AppendLine("4. Always use proper table aliases");
        sb.AppendLine("5. Use 'VNĐ' for currency formatting in column aliases");
        sb.AppendLine("6. Use GETDATE() for current date");
        sb.AppendLine("7. Date format: use GETDATE() for calculations like DATEADD, DATEDIFF");
        sb.AppendLine("8. Output only the SQL query - no explanations, no markdown code blocks, no comments");
        sb.AppendLine("9. Wrap the final query in a comment /* SQL_OUTPUT */ at the start and /* SQL_END */ at the end");
        sb.AppendLine();
        sb.AppendLine("### Available Tables:");

        foreach (var table in schema)
        {
            sb.AppendLine($"\n-- {table.TableName}: {table.Description}");
            foreach (var col in table.Columns)
            {
                var pk = col.IsPrimaryKey ? " [PK]" : "";
                var fk = col.IsForeignKey ? $" [FK -> {col.ReferencedTable}.{col.ReferencedColumn}]" : "";
                var nullStr = col.IsNullable ? " NULL" : " NOT NULL";
                sb.AppendLine($"  {col.ColumnName}: {col.DataType}{nullStr}{pk}{fk} -- {col.FriendlyDescription}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("### Example queries:");
        sb.AppendLine("1. Top 5 khách hàng mua nhiều nhất:");
        sb.AppendLine("/* SQL_OUTPUT */ SELECT TOP 5 c.CustomerName, COUNT(so.SalesOrderID) AS [Số đơn hàng], SUM(so.TotalAmount) AS [Tổng tiền VNĐ] FROM Customers c LEFT JOIN SalesOrders so ON c.CustomerID = so.CustomerID GROUP BY c.CustomerID ORDER BY SUM(so.TotalAmount) DESC /* SQL_END */");
        sb.AppendLine();
        sb.AppendLine("2. Doanh số theo tháng:");
        sb.AppendLine("/* SQL_OUTPUT */ SELECT MONTH(so.OrderDate) AS [Tháng], YEAR(so.OrderDate) AS [Năm], SUM(so.TotalAmount) AS [Doanh thu VNĐ] FROM SalesOrders so WHERE so.OrderDate >= DATEADD(MONTH, -12, GETDATE()) GROUP BY YEAR(so.OrderDate), MONTH(so.OrderDate) ORDER BY YEAR(so.OrderDate), MONTH(so.OrderDate) /* SQL_END */");
        sb.AppendLine();
        sb.AppendLine("3. Nhân viên có doanh số cao nhất:");
        sb.AppendLine("/* SQL_OUTPUT */ SELECT TOP 10 e.FullName AS [Nhân viên], d.DepartmentName AS [Phòng ban], SUM(so.TotalAmount) AS [Doanh số VNĐ] FROM Employees e LEFT JOIN SalesOrders so ON e.EmployeeID = so.SalesEmployeeID LEFT JOIN Departments d ON e.DepartmentID = d.DepartmentID WHERE e.EmploymentType = 'FullTime' GROUP BY e.FullName, d.DepartmentName ORDER BY SUM(so.TotalAmount) DESC /* SQL_END */");

        return sb.ToString();
    }
}
