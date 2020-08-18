namespace Tc.Monitor.Engine.TcNotifier
{
    public class FailData
    {
        public FailData(BuildType buildType, string[] usersEmails)
        {
            BuildType = buildType;
            UsersEmails = usersEmails;
        }

        public string[] UsersEmails { get; private set; }
        public BuildType BuildType { get; private set; }
    }
}
