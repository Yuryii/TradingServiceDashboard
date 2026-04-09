namespace Dashboard.Models;

public static class NotificationConfigDefaults
{
    public static readonly Dictionary<string, string> CronDefaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FIN_OVERDUE_30D"]     = "*/5 * * * *",
        ["FIN_EXPENSE_PENDING"]  = "*/5 * * * *",
        ["FIN_OVER_BUDGET"]      = "*/5 * * * *",
        ["FIN_NEW_PAYMENT"]       = "*/5 * * * *",
        ["FIN_CASHFLOW_LOW"]     = "*/5 * * * *",
        ["FIN_LARGE_INVOICE"]    = "*/5 * * * *",
        ["INV_LOW_STOCK"]        = "*/5 * * * *",
        ["INV_OUT_OF_STOCK"]     = "*/5 * * * *",
        ["INV_PO_PENDING"]       = "*/5 * * * *",
        ["INV_NEW_RECEIPT"]      = "*/5 * * * *",
        ["HR_LEAVE_PENDING"]     = "*/30 * * * *",
        ["HR_PROBATION_ENDING"] = "*/30 * * * *",
        ["HR_HIGH_TURNOVER"]     = "*/30 * * * *",
        ["HR_NEW_APPLICANT"]     = "*/30 * * * *",
        ["HR_BIRTHDAY"]          = "*/30 * * * *",
        ["HR_NEW_EMPLOYEE"]      = "*/30 * * * *",
        ["SAL_NEW_ORDER"]        = "*/10 * * * *",
        ["SAL_LARGE_ORDER"]     = "*/10 * * * *",
        ["SAL_DELIVERY_DELAYED"] = "*/10 * * * *",
        ["SAL_OPP_STAGE_CHANGED"] = "*/10 * * * *",
        ["SAL_TARGET_ACHIEVED"]  = "*/10 * * * *",
        ["SAL_NEW_CUSTOMER"]     = "*/10 * * * *",
        ["CS_NEW_TICKET"]        = "*/5 * * * *",
        ["CS_HIGH_PRIORITY"]     = "*/5 * * * *",
        ["CS_TICKET_SLA_BREACH"] = "*/5 * * * *",
        ["CS_TICKET_NO_RESPONSE"] = "*/5 * * * *",
        ["CS_LOW_SATISFACTION"]  = "*/5 * * * *",
        ["MKT_BUDGET_80"]       = "*/30 * * * *",
        ["MKT_BUDGET_EXCEEDED"]  = "*/30 * * * *",
        ["MKT_NEW_LEAD"]         = "*/30 * * * *",
        ["MKT_LEAD_CONVERTED"]   = "*/30 * * * *",
        ["MKT_LOW_ROAS"]         = "*/30 * * * *",
        ["EXE_DAILY_REPORT"]     = "0 8 * * *",
        ["EXE_KPI_ANOMALY"]      = "0 8 * * *",
        ["EXE_DAILY_DIGEST"]     = "0 8 * * *",
    };
}
