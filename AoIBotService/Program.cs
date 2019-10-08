using System.ServiceProcess;

namespace AoIBotService
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new BotService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
