using BarRaider.SdTools;

namespace ImeIndicator
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            SDWrapper.Run(args);
        }
    }
}