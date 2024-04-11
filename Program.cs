using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace CryptoDexNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
#if __DEBUG
           // log.Info("Debug Started");
            AutoExport service = new AutoExport();
            Console.WriteLine("Starting...");
            service.onDebug();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            Console.WriteLine("System stopped");
        }
#else
            if (args.Length == 0)
            {
                // Run your service normally.
                ServiceBase[] ServicesToRun = new ServiceBase[] { new CryptoDexNotifier() };
                ServiceBase.Run(ServicesToRun);
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-install":

                        InstallService();
                        StartService();

                        break;
                    case "-uninstall":

                        StopService();
                        UninstallService();

                        break;
                    case "-debug":

                        CryptoDexNotifier service = new CryptoDexNotifier();
                        Console.WriteLine("Starting...");
                        service.onDebug();
                        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                        Console.WriteLine("System stopped");

                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

#endif

        private static bool IsInstalled()
        {
            using (ServiceController controller =
                new ServiceController("CryptoDexNotifier"))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        private static bool IsRunning()
        {
            using (ServiceController controller =
                new ServiceController("CryptoDexNotifier"))
            {
                if (!IsInstalled()) return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }

        private static AssemblyInstaller GetInstaller()
        {
            AssemblyInstaller installer = new AssemblyInstaller(
                typeof(CryptoDexNotifier).Assembly, null);
            installer.UseNewContext = true;
            return installer;
        }

        private static void InstallService()
        {
            if (IsInstalled()) return;

            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        private static void UninstallService()
        {
            if (!IsInstalled()) return;
            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Uninstall(state);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        private static void StartService()
        {
            if (!IsInstalled()) return;

            using (ServiceController controller =
                new ServiceController("CryptoDexNotifier"))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Running)
                    {
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running,
                            TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        private static void StopService()
        {
            if (!IsInstalled()) return;
            using (ServiceController controller =
                new ServiceController("CryptoDexNotifier"))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped,
                             TimeSpan.FromSeconds(10));
                        Console.WriteLine("Stopped");
                    }
                    else
                    {
                        Console.WriteLine("Already stopped");
                    }
                }
                catch
                {
                    Console.WriteLine("Stop error");
                    throw;
                }
            }
        }
    }
}