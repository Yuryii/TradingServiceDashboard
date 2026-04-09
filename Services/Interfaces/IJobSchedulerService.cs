using Dashboard.Models.ViewModels;

namespace Dashboard.Services.Interfaces;

public interface IJobSchedulerService
{
    Task RegisterAllJobsAsync();
    void RegisterJob(string jobId, Type jobType, string cron);
    void RemoveJob(string jobId);
    void TriggerJob(string jobId);
    Task<List<JobConfigDto>> GetAllJobConfigsAsync();
    Task UpdateJobStatusAsync(int configId, bool isEnabled);
    Task UpdateCronExpressionAsync(int configId, string cronExpression);
}
