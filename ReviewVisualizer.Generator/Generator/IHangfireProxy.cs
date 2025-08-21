using System.Linq.Expressions;

namespace ReviewVisualizer.Generator.Generator
{
    public interface IHangfireProxy
    {
        void SheduleFireAndForget(Expression<Action> methodCall);
        void SheduleDelayed(string queue, Expression<Action> methodCall, TimeSpan delay);
        void SheduleRecurring(string recurringJobId, Expression<Action> methodCall, string cron);
        void RemoveIfExists(string recurringJobId);
    }
}