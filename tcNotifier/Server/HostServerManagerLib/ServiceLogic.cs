using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Tc.Monitor.Engine;
using Tc.Monitor.Engine.TcNotifier;
using Tc.Monitor.Server.DbManagerLib;
using Tc.Monitor.Server.Email;

namespace Tc.Monitor.Server.HostServerManagerLib
{
    class ServiceLogic
    {
        private readonly FailedBuildsRetriever m_FailedBuildsRetriever;
        private readonly DbManager m_DbManager = new DbManager();
        private readonly TeamAdapter m_TeamAdapter;
        private readonly string[] m_AdminEmails;
        private SpeedUpTc m_SpeedUpTc;


        public ServiceLogic()
        {
            m_TeamAdapter = GenerateTeamAdapter();
            m_AdminEmails = ReadAppSetting("AdminUserEmail").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            m_FailedBuildsRetriever = new FailedBuildsRetriever(m_TeamAdapter, m_AdminEmails);//TODO: Add the right e-mail
        }

        private static TeamAdapter GenerateTeamAdapter()
        {
            var host = ReadAppSetting("TeamCityAddress");
            var user = ReadAppSetting("User");
            var pass = ReadAppSetting("Password");
            var teamAdapterFactory = new TeamAdapterFactory(host, user, pass);
            return teamAdapterFactory.Create();
        }

        private static string ReadAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        private void CallSpeedUpTc()
        {
            Logger.Info("Starting Tc Speed-up process:");
            var pingBranch = ReadAppSetting("PingBranch");
            var pingRate = TimeSpan.FromMilliseconds(Convert.ToInt32(ReadAppSetting("PingRateMilliSeconds")));
            m_SpeedUpTc = new SpeedUpTc(m_TeamAdapter, pingBranch, pingRate);// The 'Speed - Up' thread is initiated on this creation
        }

        private void StartMainBuildsRetrieverThread()
        {
            var branches = ReadAppSetting("BranchList");
            var sleepTime = TimeSpan.FromMilliseconds(Convert.ToInt32(ReadAppSetting("SleepTimeMilliSeconds")));
            var buildsFilter = new FilterBuildsWeNeedToUpdate();
            Logger.Info("Starting the builds retrieving thread:");
            Thread thread = new Thread(() => StartFailedBuildsRetrieverLoop(branches, sleepTime, buildsFilter))
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void StartFailedBuildsRetrieverLoop(string branches, TimeSpan sleepTime, FilterBuildsWeNeedToUpdate buildsFilter)
        {
            while (true)
            {
                m_SpeedUpTc.Resume();
                var failedBuilds = m_FailedBuildsRetriever.GetFailedBuilds(branches);
                m_SpeedUpTc.Pause();

                var failedBuildsInDb = new HashSet<string>(m_DbManager.GetFailedBuildsNames());
                var newFailedBuilds = failedBuilds.Where(failedBuild => !failedBuildsInDb.Contains(failedBuild.BuildType.Id)).ToArray();
                var oldFailedBuilds = failedBuilds.Where(failedBuild => failedBuildsInDb.Contains(failedBuild.BuildType.Id)).ToArray();

                var newFailedBuildsWithOneResponsible = buildsFilter.FindResponsible(newFailedBuilds);
                var buildsIdsSentSuccessfully = SendEmailsToResponsiblesAndGetBuildsIdsToUpdate(newFailedBuildsWithOneResponsible);
                var oldFailedBuildsIds = GetFailedBulidsIdsThatAreAlreadyInDb(oldFailedBuilds, buildsIdsSentSuccessfully);

                m_DbManager.UpdateFailedBuilds(oldFailedBuildsIds);


                Thread.Sleep(sleepTime);
            }
        }

        List<string> GetFailedBulidsIdsThatAreAlreadyInDb(FailData[] oldFailedBuilds, string[] namesOfBuildsWithEmailsThatSentSuccessfully)
        {
            var oldFailedBuildsIds = oldFailedBuilds.Select(oldFailedBuild => oldFailedBuild.BuildType.Id).ToList();
            oldFailedBuildsIds.AddRange(namesOfBuildsWithEmailsThatSentSuccessfully);
            return oldFailedBuildsIds;
        }

        private string[] SendEmailsToResponsiblesAndGetBuildsIdsToUpdate(FailData[] failedBuilds)
        {
            var emailsAndTheirFailedBuilds = GetEmailsAndTheirFailedBuilds(failedBuilds);

            var buildsSuccessfullySent = new List<string>();
            foreach (var emailAndItsFailedBuilds in emailsAndTheirFailedBuilds)
            {
                var buildTypesIds = emailAndItsFailedBuilds.Value.Select(buildIdAndUrlPair => buildIdAndUrlPair.BuildType.Id).ToList();
                if (SendEmail(emailAndItsFailedBuilds.Key, emailAndItsFailedBuilds.Value))
                {
                    Logger.Info("Email sent to recipient {0}", emailAndItsFailedBuilds.Key);
                    buildsSuccessfullySent.AddRange(buildTypesIds);
                }
                else
                {
                    Logger.Info("Failed to send email to {0}, build types: {1}", emailAndItsFailedBuilds.Key, string.Join(", ", buildTypesIds));
                }
            }

            return buildsSuccessfullySent.ToArray();
        }

        private Dictionary<string, HashSet<FailData>> GetEmailsAndTheirFailedBuilds(FailData[] newFailedBuilds)
        {
            var emailsAndTheirFailedBuilds = new Dictionary<string, HashSet<FailData>>();
            foreach (var newFailedBuild in newFailedBuilds)
            {
                foreach (var userEmail in newFailedBuild.UsersEmails)//TODO: can be removed if FailData has only one email
                {
                    if (!emailsAndTheirFailedBuilds.ContainsKey(userEmail))
                        emailsAndTheirFailedBuilds.Add(userEmail, new HashSet<FailData>());

                    emailsAndTheirFailedBuilds[userEmail].Add(newFailedBuild);
                }
            }
            return emailsAndTheirFailedBuilds;
        }

        private bool SendEmail(string userEmail, HashSet<FailData> failedDatas)
        {
            var managerEmail = ActiveDirectoryHelper.GetManagerEmail(userEmail, ReadAppSetting("DomainName"));
            var cc = string.IsNullOrEmpty(managerEmail) ? null : new[] { managerEmail };
            var body = EmailBodyBuilder.Build(userEmail, failedDatas, managerEmail);

            var emailSender = new EmailManager();
            //return emailSender.SendEmail("Your Failed Builds:", body, new[] { userEmail }, cc);
            return emailSender.SendEmail("Your Failed Builds", body, m_AdminEmails, null);
        }

        public void Start()
        {
            CallSpeedUpTc();
            StartMainBuildsRetrieverThread();
        }

        public void Stop()
        {
            m_DbManager.Dispose();
        }
    }
    internal class FilterBuildsWeNeedToUpdate
    {
        public FailData[] FindResponsible(FailData[] newFailedBuilds)
        {
            var buildsWithOneResponsible = new List<FailData>();
            foreach (var newFailedBuild in newFailedBuilds)
            {
                // The last email is the responsible one
                var responsibleEmail = newFailedBuild.UsersEmails.Last();
                if (responsibleEmail.ToLower().Contains("buser"))
                {
                    for (int i = newFailedBuild.UsersEmails.Length - 1; i >= 0; --i)
                    {
                        if (!newFailedBuild.UsersEmails[i].ToLower().Contains("buser"))
                        {
                            responsibleEmail = newFailedBuild.UsersEmails[i];
                            break;
                        }
                    }
                }

                var failDataWithOneResponsible = new FailData(newFailedBuild.BuildType, new[] { responsibleEmail });
                buildsWithOneResponsible.Add(failDataWithOneResponsible);
            }
            return buildsWithOneResponsible.ToArray();
        }
    }
}
