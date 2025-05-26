using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ChildUsageEnforcer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            EnsureRunOnStartup();
            // Your normal app logic here
            Console.WriteLine("App started.");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        static void EnsureRunOnStartup()
        {
            string appName = "MyApp";
            //string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            object currentValue = key.GetValue(appName);

            if (currentValue == null || currentValue.ToString() != $"\"{exePath}\"")
            {
                key.SetValue(appName, $"\"{exePath}\"");
                Console.WriteLine("Registered app to run at startup.");
            }
            else
            {
                Console.WriteLine("App is already set to run at startup.");
            }
        }
    }
}
