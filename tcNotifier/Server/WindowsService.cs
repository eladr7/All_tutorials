using System.ServiceProcess;
using Tc.Monitor.Server.HostServerManagerLib;

namespace Tc.Monitor.Server
{
    public partial class WindowsService : ServiceBase
    {
        private readonly ServiceLogic m_ServiceLogic = new ServiceLogic();
        public WindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //WebApp.Start<Startup>(url: ReadAppSetting("HostServer"));//TODO: Add the case where no builds have been retrieved yet!
            m_ServiceLogic.Start();
        }

        protected override void OnStop()
        {
            m_ServiceLogic.Stop();
        }

        public void StartService()
        {
            OnStart(null);
        }
    }

    
}
