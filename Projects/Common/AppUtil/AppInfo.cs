using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Reflection;
using Microsoft.Phone.Tasks;

namespace Naboo.AppUtil
{
    public class AppInfo
    {
        private Assembly _appAssembly = null;
        private AppLicense _appLicense = null;

        public AppInfo(Assembly appAssembly, AppLicense appLicense)
        {
            _appAssembly = appAssembly;
            _appLicense = appLicense;
        }

        public static bool IsNetworkConnected()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }

        public static bool IsWifiConnected()
        {
            return IsNetworkConnected() &&
                    Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType == Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Wireless80211;
        }

        public String GetAppVersionString()
        {
            var asm = _appAssembly == null ? System.Reflection.Assembly.GetCallingAssembly() : _appAssembly;
            var parts = asm.FullName.Split(',');
            return parts[1].Split('=')[1];
        }

        public String GetAppMajorVersionString()
        {
            String fullVer = GetAppVersionString();

            String[] parts = fullVer.Split('.');
            if (parts.Length >= 2)
            {
                return parts[0] + "." + parts[1];
            }
            else
            {
                return fullVer;
            }
        }

        public static uint ConvertVersionToInt(String version, int numOfDigits)
        {
            uint result = 0;
            String[] vers = version.Split('.');

            uint i = 0;
            while (i < numOfDigits && i < vers.Length)
            {
                uint verDigit = 0;
                UInt32.TryParse(vers[i], out verDigit);
                result = result * 100 + verDigit;
                i++;
            }

            while (i < numOfDigits)
            {
                result = result * 100;
                i++;
            }

            return result;
        }

        public String GetFullVerstionText()
        {
            String result = GetAppVersionString();

            if (_appLicense.IsFreeApp)
            {
                result += "-Free";
            }
            else if (_appLicense.IsTrial)
            {
                result += "-Trial";
            }

#if NODO
            result += "-NoDo";
#endif

            return result;
        }

        public void SendFeedback(String subject)
        {
            SendFeedback(subject, "");
        }

        public void SendFeedback(String subject, String message)
        {
            EmailComposeTask emailTask = new EmailComposeTask();

            emailTask.To = "gydongbiz@live.com";
            emailTask.Subject = subject;

            if (!String.IsNullOrEmpty(message))
            {
                emailTask.Body = message + "\n\n";
            }
            else
            {
                emailTask.Body = "";
            }

            emailTask.Body += "\n\n\nApp Version: " + GetFullVerstionText();
            emailTask.Body += "\nDevice Information:" +
#if !NODO
 "\n\tDevice: " + Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer +
                              " " + Microsoft.Phone.Info.DeviceStatus.DeviceName +
                              "\n\tHardwareVer: " + Microsoft.Phone.Info.DeviceStatus.DeviceHardwareVersion +
                              "\n\tFirmwareVer: " + Microsoft.Phone.Info.DeviceStatus.DeviceFirmwareVersion +
                              "\n\tTotalMemory: " + Microsoft.Phone.Info.DeviceStatus.DeviceTotalMemory +
#endif
 "\n\tOSVer: " + Environment.OSVersion.ToString();


            emailTask.Show();
        }
    }
}
