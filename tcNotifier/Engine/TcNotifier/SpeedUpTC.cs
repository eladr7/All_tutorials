using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tc.Monitor.Engine.TcNotifier
{
    public class SpeedUpTc
    {
        private readonly TeamAdapter m_TeamAdapter;
        private readonly string m_PingBranch;
        private readonly TimeSpan m_PingRate;

        private readonly ManualResetEvent m_ManualResetEvent = new ManualResetEvent(false);
        public SpeedUpTc(TeamAdapter teamAdapter, string pingBranch, TimeSpan pingRate)
        {
            m_TeamAdapter = teamAdapter;
            m_PingBranch = pingBranch;
            m_PingRate = pingRate;

            var speedUpTcThread = new Thread(PingTc) { IsBackground = true };
            speedUpTcThread.Start();
        }

        private void PingTc()
        {
            while (true)
            {
                m_ManualResetEvent.WaitOne();
                m_TeamAdapter.DynamicGet("/app/rest/projects/id:{0}", m_PingBranch);
                Thread.Sleep(m_PingRate);
            }
        }

        public void Resume()
        {
            m_ManualResetEvent.Set();
        }

        public void Pause()
        {
            m_ManualResetEvent.Reset();
        }
    }
}
