using Dashboard.Models.ViewModels;
using Dashboard.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dashboard.Services;

public class PdfReportService : IPdfReportService
{
    private readonly ILogger<PdfReportService> _logger;
    private object? _vm;

    public PdfReportService(ILogger<PdfReportService> logger) => _logger = logger;

    public Task<byte[]> GenerateExecutivePdfAsync(ExecutiveDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "Executive Dashboard", BuildExecutive);
    public Task<byte[]> GenerateSalesPdfAsync(SalesDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "Sales Dashboard", BuildSales);
    public Task<byte[]> GenerateMarketingPdfAsync(MarketingDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "Marketing Dashboard", BuildMarketing);
    public Task<byte[]> GenerateInventoryPdfAsync(InventoryDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "Inventory Dashboard", BuildInventory);
    public Task<byte[]> GenerateFinancePdfAsync(FinanceDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "Finance Dashboard", BuildFinance);
    public Task<byte[]> GenerateHrPdfAsync(HumanResourcesDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "HR Dashboard", BuildHr);
    public Task<byte[]> GenerateCustomerServicePdfAsync(CustomerServiceDashboardViewModel vm, DateTime? from, DateTime? to)
        => Run(vm, from, to, "Customer Service Dashboard", BuildCustomerService);

    private delegate void Builder(ColumnDescriptor col, DateTime? from, DateTime? to);

    private Task<byte[]> Run<T>(T vm, DateTime? from, DateTime? to, string title, Builder build) where T : class
    {
        try
        {
            _vm = vm;
            var now = DateTime.Now;
            var fromStr = from?.ToString("MMM dd, yyyy") ?? "All time";
            var toStr = to?.ToString("MMM dd, yyyy") ?? "Present";
            var subtitle = "EOH - Enterprise Operations Hub | Period: " + fromStr + " - " + toStr;
            var genTime = "Generated: " + now.ToString("MMM dd, yyyy HH:mm");

            var document = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken2).FontFamily("Arial"));

                    page.Header().Background(Colors.Indigo.Darken2).Padding(12).Column(h =>
                    {
                        h.Item().Row(rh =>
                        {
                            rh.RelativeItem().Column(ch =>
                            {
                                ch.Item().Text(title).Bold().FontSize(14).FontColor(Colors.White);
                                ch.Item().Text(subtitle).FontSize(8).FontColor(Colors.White).Light();
                            });
                            rh.AutoItem().AlignRight().Column(ch =>
                            {
                                ch.Item().AlignRight().Text(genTime).FontSize(8).FontColor(Colors.White).Light();
                            });
                        });
                    });

                    page.Content().Background(Colors.Grey.Lighten4).Padding(10).Column(c => { build(c, from, to); });

                    page.Footer().Background(Colors.Grey.Lighten3).Padding(8).Row(rf =>
                    {
                        rf.RelativeItem().Text("Dashboard-X | Enterprise Operations Hub | Confidential").FontSize(8).FontColor(Colors.Grey.Darken1);
                        rf.AutoItem().AlignRight().Text(tx => { tx.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1); tx.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1); tx.Span(" / ").FontSize(8).FontColor(Colors.Grey.Darken1); tx.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1); });
                    });
                });
            });

            var bytes = document.GeneratePdf();

            _vm = null;
            return Task.FromResult(bytes);
        }
        catch (Exception ex)
        {
            _vm = null;
            _logger.LogError(ex, "Error generating PDF for {Title}", title);
            throw;
        }
    }

    // ===== HELPERS =====
    private static string V(string? s) => s ?? "-";
    private static Color Tc(string? t) => t?.ToLower() switch { "up" => Colors.Green.Darken1, "down" => Colors.Red.Darken1, _ => Colors.Grey.Darken1 };
    private static string Ti(string? t) => t?.ToLower() switch { "up" => "+", "down" => "-", _ => "=" };

    private void KpiRow(ColumnDescriptor col, params KpiCardDto[] ks)
    {
        col.Item().Row(r =>
        {
            foreach (var k in ks)
            {
                if (k == null) { r.RelativeItem(); continue; }
                r.RelativeItem().Background(Colors.White).Padding(8).Column(c =>
                {
                    c.Item().Text(V(k.Title)).FontSize(8).FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(2).Text(V(k.Value)).FontSize(13).Bold().FontColor(Colors.Grey.Darken3);
                    c.Item().Text(Ti(k.Trend) + " " + V(k.TrendLabel)).FontSize(8).FontColor(Tc(k.Trend));
                });
            }
            for (int i = ks.Length; i < 4; i++) r.RelativeItem();
        });
    }

    private void Sec(ColumnDescriptor col, string title, Action<ColumnDescriptor> body)
    {
        col.Item().Background(Colors.White).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
        {
            c.Item().BorderBottom(2).BorderColor(Colors.Indigo.Lighten2).PaddingBottom(4).Text(title).Bold().FontSize(10).FontColor(Colors.Indigo.Darken2);
            body(c);
        });
    }

    private void C3(ColumnDescriptor col, Action<ColumnDescriptor> body) { col.Item().Row(r => { r.RelativeItem().Column(body); r.RelativeItem().Column(body); }); }

    private void CTable(ColumnDescriptor col, ChartDataDto? ch)
    {
        if (ch?.Series == null || !ch.Series.Any()) { col.Item().Padding(8).Text("No data available.").Italic().FontColor(Colors.Grey.Darken1); return; }
        var cats = ch.Categories ?? new List<string>();
        int n = ch.Series.Count;
        int max = ch.Series.Max(s => s.Data?.Count ?? 0);
        if (max == 0) { col.Item().Padding(8).Text("No data available.").Italic().FontColor(Colors.Grey.Darken1); return; }
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c => { c.RelativeColumn(3); for (int i = 0; i < n; i++) c.RelativeColumn(1); });
            t.Header(h =>
            {
                h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text("Category").Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
                foreach (var s in ch.Series) h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(V(s.Name)).Bold().FontSize(8).FontColor(Colors.Grey.Darken3);
            });
            for (int i = 0; i < max; i++)
            {
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                var label = i < cats.Count ? cats[i] : "Item " + (i + 1);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
                foreach (var s in ch.Series)
                {
                    var d = s.Data;
                    var val = d != null && i < d.Count ? d[i].ToString("N0") : "-";
                    t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(val).FontSize(8).FontColor(Colors.Grey.Darken2);
                }
            }
        });
    }

    private void TTable(ColumnDescriptor col, ChartDataDto? ch)
    {
        if (ch?.Series == null || !ch.Series.Any()) { col.Item().Padding(8).Text("No data available.").Italic().FontColor(Colors.Grey.Darken1); return; }
        var cats = ch.Categories ?? new List<string>();
        var ser = ch.Series.First();
        var data = ser.Data ?? new List<decimal>();
        var items = cats.Select((c, i) => (c, i < data.Count ? data[i] : 0m)).OrderByDescending(x => x.Item2).Take(10).ToList();
        var hdr = V(ser.Name);
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
            t.Header(h => { h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(hdr).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text("Value").Bold().FontSize(8).FontColor(Colors.Grey.Darken3); });
            for (int i = 0; i < items.Count; i++)
            {
                var (name, val) = items[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(name).FontSize(8).FontColor(Colors.Grey.Darken2);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(val.ToString("N0")).FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private void LTable3(ColumnDescriptor col, string h1, string h2, string h3, IEnumerable<(string c1, string c2, string c3)> rows)
    {
        var lst = rows.ToList();
        if (!lst.Any()) { col.Item().Padding(8).Text("No data available.").Italic().FontColor(Colors.Grey.Darken1); return; }
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); c.ConstantColumn(80); });
            t.Header(h => { h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(h1).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(h2).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(h3).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); });
            for (int i = 0; i < lst.Count; i++)
            {
                var (c1, c2, c3) = lst[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(c1).FontSize(8).FontColor(Colors.Grey.Darken2);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(c2).FontSize(7).FontColor(Colors.Grey.Darken1);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(c3).FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    private void LTable4(ColumnDescriptor col, string h1, string h2, string h3, string h4, IEnumerable<(string c1, string c2, string c3, string c4)> rows)
    {
        var lst = rows.ToList();
        if (!lst.Any()) { col.Item().Padding(8).Text("No data available.").Italic().FontColor(Colors.Grey.Darken1); return; }
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(1); c.RelativeColumn(1); });
            t.Header(h => { h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(h1).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(h2).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(h3).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(h4).Bold().FontSize(8).FontColor(Colors.Grey.Darken3); });
            for (int i = 0; i < lst.Count; i++)
            {
                var (c1, c2, c3, c4) = lst[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(c1).FontSize(8).FontColor(Colors.Grey.Darken2);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).Text(c2).FontSize(8).FontColor(Colors.Grey.Darken2);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(c3).FontSize(8).FontColor(Colors.Grey.Darken2);
                t.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(4).AlignRight().Text(c4).FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    // ===== EXECUTIVE =====
    private void BuildExecutive(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not ExecutiveDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalRevenue, k.TotalExpenses, k.NetProfit, k.TotalEmployees);
        KpiRow(col, k.GrossMargin, k.CashFlow, k.Ebitda, k.NpsScore);
        if (k.CompanyPerformanceChart?.Series?.Any() == true) Sec(col, "Company Performance", c => CTable(c, k.CompanyPerformanceChart));
        if (k.RevenueProfitAreaChart?.Series?.Any() == true) Sec(col, "Revenue & Profit Trend", c => CTable(c, k.RevenueProfitAreaChart));
        if (k.TopProductsBarChart?.Series?.Any() == true) Sec(col, "Top 10 Products by Revenue", c => TTable(c, k.TopProductsBarChart));
        if (k.TopCustomersBarChart?.Series?.Any() == true) Sec(col, "Top 10 Customers by Revenue", c => TTable(c, k.TopCustomersBarChart));
        col.Item().Row(r =>
        {
            r.RelativeItem().Column(c => Sec(c, "Department Performance", cc =>
            {
                if (k.DepartmentPerformance?.Any() == true)
                {
                    cc.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cn => { cn.RelativeColumn(3); cn.RelativeColumn(2); cn.RelativeColumn(1); });
                        t.Header(h => { h.Cell().Padding(4).Text("Department").Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Padding(4).Text("Description").Bold().FontSize(8).FontColor(Colors.Grey.Darken3); h.Cell().Padding(4).AlignRight().Text("%").Bold().FontSize(8).FontColor(Colors.Grey.Darken3); });
                        foreach (var d in k.DepartmentPerformance) { t.Cell().Padding(4).Text(V(d.DepartmentName)).FontSize(8).FontColor(Colors.Grey.Darken2); t.Cell().Padding(4).Text(V(d.Description)).FontSize(7).FontColor(Colors.Grey.Darken1); t.Cell().Padding(4).AlignRight().Text(d.PerformancePercent.ToString("N1") + "%").FontSize(8).FontColor(Colors.Grey.Darken2); }
                    });
                }
                else cc.Item().Padding(8).Text("No department data.").Italic().FontColor(Colors.Grey.Darken1);
            }));
            r.RelativeItem().Column(c => Sec(c, "Alerts & Warnings", cc =>
            {
                if (k.Alerts?.Any() == true) foreach (var a in k.Alerts) { cc.Item().Background(Colors.Red.Lighten5).BorderLeft(3).BorderColor(Colors.Red.Darken1).Padding(5).PaddingLeft(8).Column(ac => { ac.Item().Text(V(a.Message)).FontSize(8).FontColor(Colors.Grey.Darken3); ac.Item().Text(V(a.Source) + "  " + a.Timestamp.ToString("MMM dd HH:mm")).FontSize(7).FontColor(Colors.Grey.Darken1); }); cc.Item().PaddingBottom(3); }
                else cc.Item().Padding(8).Text("No active alerts.").Italic().FontColor(Colors.Grey.Darken1);
            }));
        });
    }

    // ===== SALES =====
    private void BuildSales(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not SalesDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalSales, k.TotalOrders, k.NewCustomers, k.WinRate);
        KpiRow(col, k.AverageOrderValue, k.GrossMargin, k.SalesTargetAchievement, k.PendingDeals);
        if (k.SalesOverviewChart?.Series?.Any() == true) Sec(col, "Sales Overview", c => CTable(c, k.SalesOverviewChart));
        if (k.RevenueBySalespersonChart?.Series?.Any() == true) Sec(col, "Revenue by Salesperson", c => TTable(c, k.RevenueBySalespersonChart));
        if (k.RevenueByChannelChart?.Series?.Any() == true) Sec(col, "Revenue by Channel", c => CTable(c, k.RevenueByChannelChart));
        col.Item().Row(r =>
        {
            r.RelativeItem().Column(c => Sec(c, "Top Products", cc =>
            {
                if (k.TopProducts?.Any() == true) LTable3(cc, "Product", "Category", "Revenue", k.TopProducts.Take(10).Select(p => (V(p.Name), V(p.Subtitle), V(p.FormattedValue))));
                else cc.Item().Padding(8).Text("No product data.").Italic().FontColor(Colors.Grey.Darken1);
            }));
            r.RelativeItem().Column(c => Sec(c, "Top Salespersons", cc =>
            {
                if (k.TopSalespersons?.Any() == true) LTable3(cc, "Salesperson", "Position", "Revenue", k.TopSalespersons.Take(10).Select(p => (V(p.Name), V(p.Subtitle), V(p.FormattedValue))));
                else cc.Item().Padding(8).Text("No salesperson data.").Italic().FontColor(Colors.Grey.Darken1);
            }));
        });
        Sec(col, "Recent Orders", c =>
        {
            if (k.RecentOrders?.Any() == true) LTable4(c, "Order", "Customer", "Amount", "Date", k.RecentOrders.Take(10).Select(o => (V(o.Column1), V(o.Column3), V(o.Column4), V(o.Column2))));
            else c.Item().Padding(8).Text("No recent orders.").Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    // ===== MARKETING =====
    private void BuildMarketing(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not MarketingDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalReach, k.EngagementRate, k.NewLeads, k.Conversions);
        KpiRow(col, k.Cpl, k.Roas, k.Roi, k.Cac);
        if (k.CampaignPerformanceChart?.Series?.Any() == true) Sec(col, "Campaign Performance", c => CTable(c, k.CampaignPerformanceChart));
        if (k.ChannelPerformanceChart?.Series?.Any() == true) Sec(col, "Channel Performance", c => CTable(c, k.ChannelPerformanceChart));
        if (k.LeadTrendChart?.Series?.Any() == true) Sec(col, "Lead Trend", c => CTable(c, k.LeadTrendChart));
        Sec(col, "Active Campaigns", c =>
        {
            if (k.ActiveCampaigns?.Any() == true) LTable3(c, "Campaign", "Status", "Revenue", k.ActiveCampaigns.Take(10).Select(p => (V(p.Name), V(p.Subtitle), V(p.FormattedValue))));
            else c.Item().Padding(8).Text("No active campaigns.").Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    // ===== INVENTORY =====
    private void BuildInventory(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not InventoryDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalItems, k.StockValue, k.InboundOrders, k.OutboundOrders);
        KpiRow(col, k.LowStockCount, k.StockUtilization, k.InventoryTurnover, k.FillRate);
        if (k.StockMovementChart?.Series?.Any() == true) Sec(col, "Stock Movement", c => CTable(c, k.StockMovementChart));
        if (k.StockByCategoryChart?.Series?.Any() == true) Sec(col, "Stock by Category", c => CTable(c, k.StockByCategoryChart));
        if (k.AbcAnalysisChart?.Series?.Any() == true) Sec(col, "ABC Analysis", c => CTable(c, k.AbcAnalysisChart));
        col.Item().Row(r =>
        {
            r.RelativeItem().Column(c => Sec(c, "Low Stock Alerts", cc =>
            {
                if (k.LowStockItems?.Any() == true) LTable3(cc, "Product", "SKU", "Status", k.LowStockItems.Take(10).Select(p => (V(p.Name), "SKU: " + V(p.Subtitle), "LOW STOCK")));
                else cc.Item().Padding(8).Text("No low stock items.").Italic().FontColor(Colors.Grey.Darken1);
            }));
            r.RelativeItem().Column(c => Sec(c, "Warehouse Status", cc =>
            {
                if (k.WarehouseStatus?.Any() == true) LTable3(cc, "Warehouse", "Location", "Stock Value", k.WarehouseStatus.Take(10).Select(p => (V(p.Name), V(p.Subtitle), V(p.FormattedValue))));
                else cc.Item().Padding(8).Text("No warehouse data.").Italic().FontColor(Colors.Grey.Darken1);
            }));
        });
    }

    // ===== FINANCE =====
    private void BuildFinance(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not FinanceDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalIncome, k.TotalExpenses, k.NetProfit, k.CashFlow);
        KpiRow(col, k.ProfitMargin, k.Dso, k.Dpo, k.ArBalance);
        if (k.FinancialOverviewChart?.Series?.Any() == true) Sec(col, "Financial Overview", c => CTable(c, k.FinancialOverviewChart));
        if (k.CashflowTrendChart?.Series?.Any() == true) Sec(col, "Cash Flow Trend", c => CTable(c, k.CashflowTrendChart));
        Sec(col, "Expense Breakdown", c =>
        {
            if (k.ExpenseBreakdown?.Any() == true) LTable3(c, "Category", "Type", "Amount", k.ExpenseBreakdown.Take(10).Select(p => (V(p.Name), V(p.Subtitle), V(p.FormattedValue))));
            else c.Item().Padding(8).Text("No expense data.").Italic().FontColor(Colors.Grey.Darken1);
        });
        Sec(col, "Recent Transactions", c =>
        {
            if (k.RecentTransactions?.Any() == true) LTable4(c, "Date", "Description", "Category", "Amount", k.RecentTransactions.Take(10).Select(t => (V(t.Column1), V(t.Column2), V(t.Column3), V(t.Column4))));
            else c.Item().Padding(8).Text("No recent transactions.").Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    // ===== HR =====
    private void BuildHr(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not HumanResourcesDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalEmployees, k.Departments, k.NewHires, k.OpenPositions);
        KpiRow(col, k.RetentionRate, k.TurnoverRate, k.PendingLeaveRequests, k.AvgSalary);
        col.Item().Row(r =>
        {
            r.RelativeItem().Column(c => Sec(c, "Department Distribution", cc =>
            {
                if (k.DepartmentDistribution?.Any() == true) LTable3(cc, "Department", "Headcount", "Staff", k.DepartmentDistribution.Take(10).Select(p => (V(p.Name), V(p.Subtitle), V(p.FormattedValue))));
                else cc.Item().Padding(8).Text("No department data.").Italic().FontColor(Colors.Grey.Darken1);
            }));
            r.RelativeItem().Column(c => Sec(c, "Recent Hires", cc =>
            {
                if (k.RecentHires?.Any() == true) LTable3(cc, "Employee", "Position", "Joined", k.RecentHires.Take(10).Select(h => (V(h.Column1), V(h.Column2), V(h.Column3))));
                else cc.Item().Padding(8).Text("No recent hires.").Italic().FontColor(Colors.Grey.Darken1);
            }));
        });
        Sec(col, "Pending Leave Requests", c =>
        {
            if (k.LeaveRequests?.Any() == true) LTable3(c, "Employee", "Type", "Status", k.LeaveRequests.Take(10).Select(l => (V(l.Column1), V(l.Column2), V(l.Column3))));
            else c.Item().Padding(8).Text("No pending leave requests.").Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    // ===== CUSTOMER SERVICE =====
    private void BuildCustomerService(ColumnDescriptor col, DateTime? from, DateTime? to)
    {
        if (_vm is not CustomerServiceDashboardViewModel vm) return;
        var k = vm;
        KpiRow(col, k.TotalTickets, k.Satisfaction, k.ResolvedTickets, k.PendingTickets);
        KpiRow(col, k.FirstResponseRate, k.AvgResolutionTime, k.OpenTickets, k.AvgResponseTime);
        if (k.SupportOverviewChart?.Series?.Any() == true) Sec(col, "Support Overview", c => CTable(c, k.SupportOverviewChart));
        if (k.TicketVolumeTrendChart?.Series?.Any() == true) Sec(col, "Ticket Volume Trend", c => CTable(c, k.TicketVolumeTrendChart));
        col.Item().Row(r =>
        {
            r.RelativeItem().Column(c => Sec(c, "Recent Tickets", cc =>
            {
                if (k.RecentTickets?.Any() == true) LTable3(cc, "Subject", "Customer", "Status", k.RecentTickets.Take(10).Select(t => (V(t.Column1), V(t.Column2), V(t.Column3))));
                else cc.Item().Padding(8).Text("No recent tickets.").Italic().FontColor(Colors.Grey.Darken1);
            }));
            r.RelativeItem().Column(c => Sec(c, "Top Agents", cc =>
            {
                if (k.TopAgents?.Any() == true) LTable3(cc, "Agent", "Role", "Score", k.TopAgents.Take(10).Select(a => (V(a.Name), V(a.Subtitle), V(a.FormattedValue))));
                else cc.Item().Padding(8).Text("No agent data.").Italic().FontColor(Colors.Grey.Darken1);
            }));
        });
    }
}
