namespace CashApp.Application.Dashboard.Dtos;

public record CashSessionWidgetDto(
    int Id,
    int CashRegisterId,
    string CashRegisterName,
    DateTime OpenedAt,
    decimal OpeningBalance,
    decimal CurrentTheoreticalBalance,
    int OperationCount);

public record OperationWidgetDto(
    int Id,
    string OperationRef,
    DateTime OperationDate,
    string Direction,
    string CategoryLabel,
    decimal Amount,
    string Label);

// Point de tendance quotidienne, utilisé pour les graphiques d'évolution (courbe / barres).
public record DailyTrendPointDto(
    DateTime Date,
    decimal TotalIn,
    decimal TotalOut,
    decimal Net);

// Répartition d'un montant/nombre d'opérations par catégorie, pour les graphiques camembert/barres.
public record CategoryBreakdownDto(
    string CategoryLabel,
    decimal Amount,
    int OperationCount);

// Répartition par caisse (net du jour), pour comparer l'activité des caisses.
public record RegisterBreakdownDto(
    int CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    decimal Net,
    int OperationCount);

public record CashierDashboardDto(
    CashSessionWidgetDto? ActiveSession,
    decimal TodayTotalIn,
    decimal TodayTotalOut,
    int TodayOperationCount,
    IReadOnlyList<OperationWidgetDto> RecentOperations,
    IReadOnlyList<DailyTrendPointDto> Trend7Days,
    IReadOnlyList<CategoryBreakdownDto> TodayCategoryBreakdown);

public record SupervisorKpiDto(string Label, decimal Value, string? Unit);

public record SupervisorDashboardDto(
    int OpenSessionsCount,
    int TodayClosedSessionsCount,
    decimal TodayTotalIn,
    decimal TodayTotalOut,
    decimal TodayNetMovement,
    int SessionsWithVarianceCount,
    IReadOnlyList<CashSessionWidgetDto> OpenSessions,
    IReadOnlyList<SupervisorKpiDto> Kpis,
    IReadOnlyList<DailyTrendPointDto> Trend14Days,
    IReadOnlyList<CategoryBreakdownDto> TopCategories30Days,
    IReadOnlyList<RegisterBreakdownDto> RegisterBreakdownToday,
    int PendingApprovalsCount,
    bool ApprovalsFeatureEnabled,
    int OpenVarianceCasesCount,
    bool VarianceFeatureEnabled,
    int OpenAnomaliesCount,
    bool AnomaliesFeatureEnabled);
