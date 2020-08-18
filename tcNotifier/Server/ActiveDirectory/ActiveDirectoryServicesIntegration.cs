using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace Tc.Monitor.Server.ActiveDirectory
{
    internal class ActiveDirectoryServicesIntegration 
    {
        private readonly string m_DomainName;
        private readonly PrincipalContext m_PrincipalContext;

        public ActiveDirectoryServicesIntegration(string domainName)
        {
            m_DomainName = domainName;
            m_PrincipalContext = new PrincipalContext(ContextType.Domain, domainName);
        }

        public ActiveDirectoryServicesIntegration(string domainName, string username, string password)
        {
            m_DomainName = domainName;
            m_PrincipalContext = new PrincipalContext(ContextType.Domain, domainName, username, password);
        }

        public UserDetails GetUserByEmail(string email)
        {
            UserPrincipal userPrincipal = new UserPrincipal(m_PrincipalContext)
            {
                EmailAddress = email,
            };

            PrincipalSearcher principalSearcher = new PrincipalSearcher(userPrincipal);
            UserPrincipal searchResult = principalSearcher.FindOne() as UserPrincipal;
            if (searchResult != null)
            {
                UserDetails userDetails = CreateImportedUser(searchResult);
                if (!string.IsNullOrEmpty(userDetails.DisplayName))
                    return userDetails;
            }
            return null;
        }

        public IEnumerable<UserDetails> GetUsers()
        {
            UserPrincipal userPrincipal = new UserPrincipal(m_PrincipalContext) { Name = "*" };
     
            PrincipalSearcher principalSearcher = new PrincipalSearcher(userPrincipal);
            PrincipalSearchResult<Principal> principalSearchResult = principalSearcher.FindAll();
            foreach (UserPrincipal resultUserPrincipal in principalSearchResult)
            {
                if (resultUserPrincipal != null)
                {
                    UserDetails userDetails = CreateImportedUser(resultUserPrincipal);
                    if (!string.IsNullOrEmpty(userDetails.DisplayName))
                        yield return userDetails;
                }
            }
        }

        private static UserDetails CreateImportedUser(UserPrincipal user)
        {
            var result = new UserDetails
            {
                DisplayName = user.DisplayName,
                Username = user.SamAccountName,
                Email = user.EmailAddress,
                Sid = user.Sid.Value,
                Description = user.Description,
                DistinguishedName = user.DistinguishedName,
                EmployeeId = user.EmployeeId,
                Guid = user.Guid,
                IsEnabled = user.Enabled.HasValue ? user.Enabled.Value : false,
                DomainName = user.Context.Name
            };

            var userEntry = user.GetUnderlyingObject() as DirectoryEntry;
            
            if (userEntry.Properties["manager"] != null && userEntry.Properties["manager"].Count > 0)
            {
                var managerDN = userEntry.Properties["manager"][0].ToString();
                var managerUserPrincipal = UserPrincipal.FindByIdentity(user.Context, managerDN);

                result.ManagerEmail = managerUserPrincipal.EmailAddress;
            }

            return result;
        }

        public UserDetails GetUserDetails(string username)
        {
            UserPrincipal user = UserPrincipal.FindByIdentity(m_PrincipalContext, username);
            if (user != null)
                return CreateImportedUser(user);
            return null;
        }
    }

    internal class UserDetails
    {
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Sid { get; set; }
        public string Description { get; set; }
        public string DistinguishedName { get; set; }
        public string EmployeeId { get; set; }
        public Guid? Guid { get; set; }
        public bool IsEnabled { get; set; }
        public string DomainName { get; set; }
        public string ManagerEmail { get; set; }
    }
}
