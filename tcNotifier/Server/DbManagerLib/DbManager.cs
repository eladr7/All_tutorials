using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tc.Monitor.Engine;
using Tc.Monitor.Engine.TcNotifier;

namespace Tc.Monitor.Server.DbManagerLib
{
    class DbManager : IDisposable
    {
        public void Dispose()
        {
            if (m_TcMonitorConnection != null)
            {
                m_TcMonitorConnection.Close();
                m_TcMonitorConnection = null;
            }
        }
        private SqlConnection m_TcMonitorConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["TcMonitorConnection"].ConnectionString);
        

        public DbManager()
        {
            m_TcMonitorConnection.Open();
        }

        public void UpdateFailedBuilds(List<string> namesOfFailedBuilds)
        {
            Logger.Info("Inserting {0} into The database", string.Join(", ", namesOfFailedBuilds));
            if (namesOfFailedBuilds.Count == 0)
            {
                return;
            }
            const string sqlTrunc = "TRUNCATE TABLE RedBuild";
            var cmd = new SqlCommand(sqlTrunc, m_TcMonitorConnection);
            cmd.ExecuteNonQuery();

            foreach (var failedBuildToInsert in namesOfFailedBuilds)
            {
                InsertFailedBuild(failedBuildToInsert);
            }
        }

        public List<string> GetFailedBuildsNames()
        {
            const string getFailedBuildsCmd = "Select * From RedBuild";
            var sqlCommand = new SqlCommand(getFailedBuildsCmd, m_TcMonitorConnection);
            var failedBuildsNames = new List<string>();

            using (SqlDataReader reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    failedBuildsNames.Add(reader["BuildTypeId"].ToString());
                }
            }
            return failedBuildsNames;
        }

        public void InsertFailedBuild(string buildTypeId)
        {
            const string insertBuildCmd = "INSERT INTO RedBuild VALUES (@buildTypeId)";
            ExecuteSqlCommand("@buildTypeId", buildTypeId, insertBuildCmd);//TODO: Add the ability to insert several parameters    (read about params c#)
        }

        private void ExecuteSqlCommand(string parameterName, string parameterValue, string commandString)
        {
            var sqlCommand = new SqlCommand(commandString, m_TcMonitorConnection);
            sqlCommand.Parameters.AddWithValue(parameterName, parameterValue);
            sqlCommand.ExecuteNonQuery();
        }
    }
}