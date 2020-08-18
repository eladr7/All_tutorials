using System.Collections.Generic;
using System.Text;
using Tc.Monitor.Engine.TcNotifier;

namespace Tc.Monitor.Server.Email
{
    static class EmailBodyBuilder
    {
        public static string Build(string userEmail, HashSet<FailData> failedDatas, string managerEmail)
        {
            const string bodyFormat =
                @"<html>
                <body>
                Hi {0}
                <br/>
                <br/>
                The following build(s) failed:
                <br/>
                {1}
                </body>
                </html>";

            var webUrls = new StringBuilder();

            foreach (var failData in failedDatas)
            {
                webUrls.Append(failData.BuildType.WebUrl);
                webUrls.Append("<br/>");
            }

            var body = string.Format(bodyFormat, userEmail, webUrls);
            return body;
        }
    }
}
