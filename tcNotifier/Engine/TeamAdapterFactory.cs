namespace Tc.Monitor.Engine
{
    public class TeamAdapterFactory
    {
        private readonly string m_Address;
        private readonly string m_User;
        private readonly string m_Pass;

        public TeamAdapterFactory(string address, string user, string pass)
        {
            m_Address = address;
            m_User = user;
            m_Pass = pass;
        }

        public TeamAdapter Create()
        {
            var tc = new RestClient(m_Address, m_User, m_Pass);
            var teamAdapter = new TeamAdapter(tc);
            return teamAdapter;
        }
    }
}
