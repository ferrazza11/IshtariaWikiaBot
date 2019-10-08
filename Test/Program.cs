using IshtariaWikiaBot;
using System;
using System.Timers;

namespace Test
{
    class Program
    {       
        static void Main(string[] args)
        {
            Console.WriteLine("User?");
            string user = Console.ReadLine();
            Console.WriteLine("Password?");
            string pwd = Console.ReadLine();
            Boti.DoWork(user, pwd);
            Console.ReadLine();
        }
    }
    public static class Boti
    {
        public static void DoWork(string user, string pwd)
        {
            Bot b = new Bot();
            b.Start(user, pwd);
        }       
    }
}
