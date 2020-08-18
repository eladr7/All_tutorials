using System;
using System.Net;
using System.Net.Mail;
using Tc.Monitor.Engine;

namespace Tc.Monitor.Server.Email
{
    public class EmailManager
    {
        private static string EmailFrom { get { return GetValueFromAppSettings("EmailSender.From", "noreply@qualisystems.com"); } }
        private static int EmailSmtpPort { get { return GetValueFromAppSettings("EmailSender.Port", 587); } }
        private static bool EmailSmtpEnableSsl { get { return GetValueFromAppSettings("EmailSender.EnableSsl", true); } }
        private static string EmailSmtpUser { get { return GetValueFromAppSettings("EmailSender.User", "noreply@qualisystems.com"); } }
        private static string EmailSmtpPassword { get { return GetValueFromAppSettings("EmailSender.Password", "Qu@li1234"); } }
        private static string EmailSmtpServer { get { return GetValueFromAppSettings("EmailSender.Server", "Smtp.outlook.com"); } }

        private static T GetValueFromAppSettings<T>(string key, T defaultValue)
        {
            // For now reading from app.config is not implemented
            return defaultValue;
        }

        public bool SendEmail(string subject, string body, string[] recipients, string[] cc = null, bool isBodyHtml = true, string attachments = null)
        {
            var credential = new NetworkCredential(EmailSmtpUser, EmailSmtpPassword);
            var mailComposer = new EmailSender(EmailSmtpServer, EmailSmtpPort, EmailSmtpEnableSsl, credential);

            MailAddress from;

            if (EmailFrom.Contains(";"))
            {
                var split = EmailFrom.Split(';');
                from = new MailAddress(split[0], split[1]);
            }
            else
            {
                from = new MailAddress(EmailFrom);
            }

            using (var message = new MailMessage(from, new MailAddress(recipients[0])))
            {
                for (var i = 1; i < recipients.Length; i++)
                    message.To.Add(recipients[i]);

                if (cc != null)
                {
                    foreach (var ccEmail in cc)
                        message.CC.Add(ccEmail);
                }

                message.Subject = subject;
                message.IsBodyHtml = isBodyHtml;
                message.Body = body;

                //if (!string.IsNullOrEmpty(attachments))
                //{
                //    foreach (var file in GetAttachmentFiles(attachments))
                //        message.Attachments.Add(new Attachment(file));
                //}
                try
                {
                    mailComposer.Send(message);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("email to {0} was failed to sent: {1}", recipients, ex));
                    return false;
                }
            }
        }

        //private string[] GetAttachmentFiles(string attachments)
        //{
        //    var files = new List<string>();

        //    foreach (var attachment in attachments.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        //    {
        //        if (attachment.Contains("*"))
        //        {
        //            var searchPattern = Path.GetFileName(attachment);
        //            var folder = Path.GetDirectoryName(attachment);
        //            files.AddRange(m_FileSystem.GetFiles(folder, searchPattern));
        //        }
        //        else
        //        {
        //            files.Add(attachment);
        //        }
        //    }

        //    foreach (var file in files)
        //    {
        //        if (!m_FileSystem.FileExists(file))
        //        {
        //            m_Logger.ErrorFormat("Attachment file not found: " + file);
        //            throw new QsBuildFailedException("Attachment file not found");
        //        }
        //    }

        //    return files.ToArray();
        //}
    }
}