using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.WebApi.Processor
{
    public interface IProcessorHost
    {
        bool CreateAnalyst(Analyst analyst);
        void Init();
        void OnWorkerStopped(object sender, EventArgs e);
        void Start();
        bool StartAnalyst(int id);
        bool StopAnalyst(int id);
    }
}