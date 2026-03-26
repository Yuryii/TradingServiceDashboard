namespace Dashboard.Models.ViewModels;

public class KpiCardDto
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string FormattedValue { get; set; } = string.Empty;
    public decimal NumericValue { get; set; }
    public decimal GrowthPercent { get; set; }
    public string Trend { get; set; } = "neutral"; // up, down, neutral
    public string TrendLabel { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = "bg-label-primary";
    public string IconColorClass { get; set; } = "text-primary";
    public string Subtitle { get; set; } = string.Empty;
}

public class ChartSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
}

public class ChartDataDto
{
    public List<string> Categories { get; set; } = new();
    public List<ChartSeriesDto> Series { get; set; } = new();
    public string ChartTitle { get; set; } = string.Empty;
    public string ChartType { get; set; } = "bar";
}

public class BulletChartDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Actual { get; set; }
    public decimal Target { get; set; }
    public decimal Forecast { get; set; }
    public decimal ActualPercent { get; set; }
    public string Status { get; set; } = "on-track"; // on-track, warning, danger
}

public class WaterfallItemDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Color { get; set; } = "primary"; // primary, success, danger, warning
    public bool IsTotal { get; set; }
}

public class HeatmapCellDto
{
    public string X { get; set; } = string.Empty;
    public string Y { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class HeatmapDto
{
    public List<string> XCategories { get; set; } = new();
    public List<string> YCategories { get; set; } = new();
    public List<HeatmapCellDto> Data { get; set; } = new();
}

public class TopNItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal SecondaryValue { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = "bg-label-primary";
}

public class FunnelStageDto
{
    public string Stage { get; set; } = string.Empty;
    public decimal Count { get; set; }
    public decimal Value { get; set; }
    public decimal ConversionRate { get; set; }
}

public class AlertDto
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, danger, success
    public string Severity { get; set; } = "green"; // green, yellow, red
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TableRowDto
{
    public string Column1 { get; set; } = string.Empty;
    public string Column2 { get; set; } = string.Empty;
    public string Column3 { get; set; } = string.Empty;
    public string Column4 { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public string TrendClass { get; set; } = string.Empty;
}

public class ActivityDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = "bg-label-primary";
    public DateTime Timestamp { get; set; }
}

public class DepartmentPerformanceDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PerformancePercent { get; set; }
    public string IconClass { get; set; } = string.Empty;
    public string IconBgClass { get; set; } = "bg-label-primary";
}

public class ScatterPointDto
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Size { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class CohortDataDto
{
    public string Cohort { get; set; } = string.Empty;
    public List<decimal> Values { get; set; } = new();
}

public class SparklineDto
{
    public List<decimal> Data { get; set; } = new();
    public string Color { get; set; } = "primary";
}

// Extended ChartDataDto with additional properties for specialized charts
public class ScatterChartDto
{
    public List<string> SeriesNames { get; set; } = new();
    public List<ScatterSeriesData> Series { get; set; } = new();
}

public class ScatterSeriesData
{
    public string Name { get; set; } = string.Empty;
    public List<ScatterPointData> Data { get; set; } = new();
}

public class ScatterPointData
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
}

public class FunnelChartDto
{
    public List<string> Categories { get; set; } = new();
    public List<ChartSeriesDto> Series { get; set; } = new();
}

public class TreemapChartDto
{
    public List<TreemapSeriesDto> Series { get; set; } = new();
}

public class TreemapSeriesDto
{
    public List<TreemapDataDto> Data { get; set; } = new();
}

public class TreemapDataDto
{
    public string X { get; set; } = string.Empty;
    public decimal Y { get; set; }
}

public class BoxPlotSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<BoxPlotDataDto> Data { get; set; } = new();
}

public class BoxPlotDataDto
{
    public decimal Low { get; set; }
    public decimal Q1 { get; set; }
    public decimal Median { get; set; }
    public decimal Q3 { get; set; }
    public decimal High { get; set; }
}

public class ParetoChartDto
{
    public List<string> Categories { get; set; } = new();
    public List<ChartSeriesDto> Series { get; set; } = new();
    public List<decimal> CumulativePercent { get; set; } = new();
}

public class RevenueByRegionMapDto
{
    public List<MapSeriesDto> Series { get; set; } = new();
}

public class MapSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<MapDataDto> Data { get; set; } = new();
}

public class MapDataDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class WaterfallChartDto
{
    public List<string> Categories { get; set; } = new();
    public List<WaterfallItemDto> Items { get; set; } = new();
}

public class AlertIndicatorDto
{
    public string Metric { get; set; } = string.Empty;
    public decimal Actual { get; set; }
    public decimal Target { get; set; }
    public string Status { get; set; } = "green"; // green, yellow, red
    public string Message { get; set; } = string.Empty;
}
