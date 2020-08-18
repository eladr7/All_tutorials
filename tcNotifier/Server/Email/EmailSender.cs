using System.Net;
using System.Net.Mail;

namespace Tc.Monitor.Server.Email
{
    public class EmailSender
    {
        private readonly SmtpClient m_SmtpClient;

        public EmailSender(string smtpServer, int smtpPort, bool useDefaultCredentials = false, ICredentialsByHost credentials = null)
        {
            m_SmtpClient = new SmtpClient(smtpServer, smtpPort);
            m_SmtpClient.UseDefaultCredentials = useDefaultCredentials;
            m_SmtpClient.ServicePoint.MaxIdleTime = 1000; //this value in milliseconds, cause to mail be sent immediately

            if (credentials != null)
                m_SmtpClient.Credentials = credentials;

        }

        public bool Send(MailMessage message)
        {
            try
            {
                m_SmtpClient.Send(message);
                return true;
            }
            finally
            {
                message.Dispose();
            }

        }
     }
}
