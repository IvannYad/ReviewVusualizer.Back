using Hangfire;
using System.Linq.Expressions;

namespace ReviewVisualizer.Generator.Generator
{
    public class HangfireProxy : IHangfireProxy
    {
        public void RemoveIfExists(string recurringJobId)
        {
            RecurringJob.RemoveIfExists(recurringJobId);
        }

        public void SheduleDelayed(string queue, Expression<Action> methodCall, TimeSpan delay)
        {
            BackgroundJob.Schedule(queue, methodCall, delay);
        }

        public void SheduleFireAndForget(Expression<Action> methodCall)
        {
            BackgroundJob.Enqueue(methodCall);
        }

        public void SheduleRecurring(string recurringJobId, Expression<Action> methodCall, string cron)
        {
            RecurringJob.AddOrUpdate(recurringJobId, methodCall, cron);
        }
    }
}