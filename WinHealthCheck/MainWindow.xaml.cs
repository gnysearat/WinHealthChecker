using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;

namespace WinHealthCheck
{

    public partial class MainWindow : Window
    {
        StringBuilder sb = new StringBuilder();
        string path = "C:\\logs\\WinHealthCheck.log";
        
        public MainWindow()
        {
            InitializeComponent();
            
        }

        //Go button, main operation of program.
        private void BtnGo_Click(object sender, RoutedEventArgs e)
        {
            clearLbls();

            if (checkExternalConnectivity())
            {
                lblExternal.Content = "Pass!";
                lblExternal.Foreground = Brushes.LightGreen;
                writeToLog("External network reachable.");

                if (checkInternalConnectivity())
                {
                    lblInternal.Content = "Pass!";
                    lblInternal.Foreground = Brushes.LightGreen;
                    writeToLog("Internal network reachable.");

                    checkIpAddress();
                    

                }
                else
                {
                    lblInternal.Content = "Fail!";
                    lblInternal.Foreground = Brushes.Red;
                }

            }
            else
            {
                lblExternal.Content = "Fail!";
                lblExternal.Foreground = Brushes.Red;
            }
            checkWindows();
            checkDomain();
            checkComputerModel();

        }

        //MAIN1a - Open up a browser, navigate to google, if it loads, return true.
        private bool checkExternalConnectivity()
        {
            WebClient client = new WebClient();
            try
            {
                using (client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                writeToLog(e.Message);
                return false;
            }
        }

        //MAIN1b - Checks to see if IP address is valid
        private void checkIpAddress()
        {
            Ping ping = new Ping();
            var reply = ping.Send(Dns.GetHostName());
            string hostname = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostname);
            IPAddress[] addresses = hostEntry.AddressList;

            if(reply.Status == IPStatus.Success)
            {
                try
                    {
                        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                        {
                            //If NIC is up, and 
                            if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                            {
                                    writeToLog(ni.Name + " is available with address: " + addresses[1].ToString());
                                    lblHostName.Content = hostname + " | " + addresses[1].ToString();
                            }
                            else
                            {
                                writeToLog(ni.Name + " is not available.");
                            }

                            }

                }

                catch
                {
                    writeToLog("Network Interface is not available.");
                    lblHostName.Content = "No IP found.";
                }
                Console.WriteLine(reply.Address);
                //lblHostName.Content = hostname + "\n" + addresses[1].ToString();
            }
            else
            {
                Console.WriteLine("No IP found.");
                lblHostName.Content = "No IP found.";
            }

        }

        //MAIN2  - Open Nordnet, if it loads return true.
        private bool checkInternalConnectivity()
        {
            WebClient client = new WebClient();
            try
            {
                using (client.OpenRead("http://nordnet/"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        //MAIN3 - Check to see if computer is on Domain
        private void checkDomain()
        {
            string domain = System.Environment.UserDomainName;
            lblDomain.Content = domain;
            writeToLog(domain);
        }

        //MAIN4 - McAfee install validation
        private void checkMcafee()
        {

        }

        //MAIN5 - Windows version/build check
        private void checkWindows()
        {
            string version = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString() + " " + Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
            lblBuild.Content = version;
            writeToLog(version);
        }

        //MAIN6 - Find computer model number
        private void checkComputerModel()
        {
            string wmiQuery = string.Format("SELECT Version FROM Win32_ComputerSystemProduct");

            ManagementObjectSearcher search = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection objCollect = search.Get();
            //ManagementObject obj = objCollect.OfType<ManagementObject>().FirstOrDefault();
            //This returns a list type object, and you need to enumerate through it in order to get the information you need. 
            //It also doesn't fucking work.
            
            foreach(ManagementObject obj in objCollect)
            {
                lblModel.Content = obj.GetPropertyValue("Version").ToString();
                writeToLog(obj.GetPropertyValue("Version").ToString());
            }
            
            
        }

        //MAIN7 - Check office activation - Not currently implemented.
        private void checkOfficeActivation()
        {
            Process prc = new Process();
            prc.StartInfo.FileName = @"cscript";
            prc.StartInfo.WorkingDirectory = @"C:\Program Files (x86)\Microsoft Office\Office16\";
            prc.StartInfo.Arguments = @"/dstatus ospp.vbs";
            prc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            prc.StartInfo.RedirectStandardOutput = true;
            prc.StartInfo.UseShellExecute = false;
            prc.StartInfo.Verb = "runas";
            prc.Start();
                        

            while(prc.StandardOutput.EndOfStream)
            {
                writeToLog("Office version: " + prc.StandardOutput.ReadLine());
                lblModel.Content = "Office version: " + prc.StandardOutput.ReadLine();
            }
            prc.WaitForExit();

            //writeToLog(prc.StandardOutput.ReadToEndAsync().ToString());
            //Need to get output from vbscript

        }

        //MISC - Method for logging to console, and log files.
        private void writeToLog(string mess)
        {
            DateTime stamp = DateTime.Now;
            Console.WriteLine(stamp.ToString() + " - " + mess);
            sb.AppendLine(stamp.ToString() + " - " + mess);
            File.WriteAllText(path, sb.ToString());
        }

        //MISC - Clear labels so you can run it multiple times.
        private void clearLbls()
        {
            lblExternal.Content = "";
            lblInternal.Content = "";
            lblConnection.Content = "";
            lblHostName.Content = "";
            lblBuild.Content = "";
            lblModel.Content = "";
            lblDomain.Content = "";
        }
    }
}
