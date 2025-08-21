namespace ReviewVisualizer.TestUtils
{
    public class PassThroughHandler : DelegatingHandler
    {
        public PassThroughHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }
    }
}