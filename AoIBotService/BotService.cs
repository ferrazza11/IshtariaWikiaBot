using System;
using System.ServiceProcess;
using System.Timers;
using IshtariaWikiaBot;

namespace AoIBotService
{
    public partial class BotService : ServiceBase
    {
        string user = "";
        string pwd = "";
        Timer tClock;
        Bot b;
        DateTime lstUpdate = DateTime.MinValue;
        public BotService()
        {   
            InitializeComponent();                        
            InitializeTimer();
        }

        void InitializeTimer()
        {
            tClock = new Timer(60000);
            tClock.Elapsed += tClock_Elapsed;
        }

        private void tClock_Elapsed(object sender, ElapsedEventArgs e)
        {
            //tClock.Stop();
            try
            {
                TimeSpan ts = (DateTime.Now - lstUpdate);
                if (ts.TotalMinutes > 60)
                {
                    lstUpdate = DateTime.Now;
                    b = new Bot();
                    b.Start(user, pwd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //tClock.Start();
        }

        protected override void OnStart(string[] args)
        {
            tClock.Start();
        }

        protected override void OnStop()
        {         
            tClock.Stop();
        }      
    }
}
