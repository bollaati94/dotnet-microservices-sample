using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;
using PublisherService;

var builder = WebApplication.CreateBuilder(args);

var intervalSeconds = builder.Configuration.GetValue<int>("SimulationIntervalSeconds", 10);

builder.Services.AddSingleton<IGatePublisher, GatePublisher>();
builder.Services.AddTransient<GateSimulationJob>();

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

// Fix 401: Allow anonymous access to Hangfire Dashboard in the sample
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new PassThroughAuthorizationFilter() }
});

RecurringJob.AddOrUpdate<GateSimulationJob>(
    "random-gate-events", 
    job => job.RunSimulation(), 
    Cron.Minutely()
);

app.MapGet("/", () => "Publisher Service is running. Check /hangfire for dashboard.");
app.Run();

// Helper class to bypass Hangfire's default authorization
public class PassThroughAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
