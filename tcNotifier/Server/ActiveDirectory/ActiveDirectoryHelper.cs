using System;
using Tc.Monitor.Server.ActiveDirectory;

namespace Tc.Monitor.Server.HostServerManagerLib
{
    internal static class ActiveDirectoryHelper
    {
        public static string GetManagerEmail(string userEmail, string domainName)
        {
            var activeDirectoryServicesIntegration = new ActiveDirectoryServicesIntegration(domainName);
            var userInfo = activeDirectoryServicesIntegration.GetUserByEmail(userEmail);
            return userInfo.ManagerEmail;
        }
    }
}