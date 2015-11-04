using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Configuration;
using System.Configuration.Install;

namespace XTRMExecutiveService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        /*
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }*/
        static void Main(string[] args)
        {
            if (System.Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new Service1());
            }
            Console.ReadKey();
        }
    }
}
