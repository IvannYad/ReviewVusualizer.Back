using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Generator
{
    public interface IGeneratorHost
    {
        public void Init();
        void Start();
        public bool CreateReviewer(Reviewer reviewer);
        public bool StopReviewer(int id);
        public bool StartReviewer(int id);
        public void OnWorkerStopped(object sender, EventArgs e);

    }
}