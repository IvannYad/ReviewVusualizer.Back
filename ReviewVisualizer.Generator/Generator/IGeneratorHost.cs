using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Generator
{
    public interface IGeneratorHost
    {
        void Init();
        void Start();
        bool CreateReviewer(Reviewer reviewer);
        bool DeleteReviewer(Reviewer reviewer);
        bool StopReviewer(int id);
        bool StartReviewer(int id);
        void OnWorkerStopped(object sender, EventArgs e);

    }
}