namespace AspnetCoreMvcFull.Models;

public class DashboardViewModel
{
    public int CustomerCount { get; init; }
    public int LeadCount { get; init; }
    public decimal PipelineValue { get; init; }
    public int OpenTaskCount { get; init; }
    public IReadOnlyList<Lead> RecentLeads { get; init; } = [];
    public IReadOnlyList<CrmTask> UpcomingTasks { get; init; } = [];
}