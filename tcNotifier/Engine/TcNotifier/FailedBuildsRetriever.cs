using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tc.Monitor.Engine.TcNotifier
{
    public class FailedBuildsRetriever
    {
        private readonly TeamAdapter m_TeamAdapter;
        private readonly string[] m_AdminEmails;

        public FailedBuildsRetriever(TeamAdapter teamAdapter, string[] adminEmails)
        {
            m_TeamAdapter = teamAdapter;
            m_AdminEmails = adminEmails;
        }

        public List<FailData> GetFailedBuilds(string rootProjectIds)
        {
            var failedBuildsFromAllBranches = new List<FailData>();
            var locker = new object();
            Parallel.ForEach(rootProjectIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries), rootProjectId =>
            {
                Console.WriteLine("Retrieving failed builds for project: " + rootProjectId);
                Logger.Info("Retrieving builds for project: " + rootProjectId);
                var branchFailedBuilds = new List<FailData>();
                try
                {
                    
                    var projects = m_TeamAdapter.GetProjects(rootProjectId, rootProjectId);
                    Logger.Info("Found {0} projects under project {1}", projects.Length, rootProjectId);
                    foreach (var project in projects)
                    {
                        var answer = GetFailedBuildsWithResponsiblesEmails(project);
                        branchFailedBuilds.AddRange(answer);
                    }
                    Logger.Info("Found {0} failed builds under project {1} with no investigator", branchFailedBuilds.Count, rootProjectId);
                    Console.WriteLine("Finished retrieving failed builds for project: " + rootProjectId);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error retrieving failed builds for project {0}: {1}", rootProjectId, ex.ToString());
                    Console.WriteLine("Error retrieving failed builds for project {0}", rootProjectId);
                }

                lock (locker)
                {
                    failedBuildsFromAllBranches.AddRange(branchFailedBuilds);
                }

            });
            
            return failedBuildsFromAllBranches;
        }

        private List<FailData> GetFailedBuildsWithResponsiblesEmails(Project project)
        {
            var result = new List<FailData>();
            foreach (var buildType in project.BuildTypes)
            {
                if (buildType.LastBuild != null && buildType.LastBuild.Status == BuildResult.Red)
                {
                    var investigator = buildType.Investigator;
                    if (investigator == null)
                    {
                        if (buildType.LastBuild.Status.ToString().Contains("Failed To Start"))//TODO: find the correct status
                        {
                            Logger.Info("Build {0} failed to start", buildType.LastBuild.Href);
                            continue;
                        }

                        var changes = buildType.LastBuild.Data.Changes; // Inside 'change' we have: user, eMail
                        var userEmails = new List<string>();
                        if (changes.Count > 0)
                        {
                            foreach (var changeData in changes)
                            {
                                var userEmail = changeData.User.GetInfo().Email;
                                if (userEmail != null)
                                {
//                                    if (userEmail.ToLower().Contains("buser"))
//                                        AddAdminEmailsToList(userEmails);
//                                    else
                                        userEmails.Add(userEmail);
                                }
                                else
                                {
                                    Logger.Info("User {0} is responsible for failed build {1} but has no email", changeData.User.Name, buildType.LastBuild.Id.ToString());
                                    AddAdminEmailsToList(userEmails);
                                }
                            }
                        }
                        else
                        {
                            AddAdminEmailsToList(userEmails);
                        }
                        var failedData = new FailData(buildType, userEmails.ToArray());

                        result.Add(failedData);
                    }
                }
            }
            return result;
        }

        private void AddAdminEmailsToList(List<string> emails)
        {
            foreach (var adminEmail in m_AdminEmails)
                emails.Add(adminEmail);
        }
    }
}
