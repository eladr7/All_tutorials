using System;
using System.Collections.Generic;
using System.Net;
using EasyHttp.Http;

namespace Tc.Monitor.Engine
{
    public class TeamAdapter
    {
        private readonly RestClient m_Client;

        internal TeamAdapter(RestClient client)
        {
            m_Client = client;
        }

        internal Build GetLastBuildByBuildTypeId(string buildTypeId)
        {
            var builds = DynamicGet("/app/rest/builds?locator=buildType:(id:{0}),count:1", buildTypeId);
            
            if (builds.build.Length == 0)
            {
                return null;
            }

            var build = builds.build[0];

            return new Build(this, (int)build.id, build.href, build.number, build.status);
        }

        internal User[] GetConanUsers()
        {
            var result = new List<User>();

            var conanGroup = DynamicGet("/app/rest/userGroups/key:CONAN");

            for (int i = 0; i < conanGroup.users.user.Length; i++)
            {
                var user = conanGroup.users.user[i];
                result.Add(new User(this, user.name, user.href));
            }

            return result.ToArray();
        }

        internal Project[] GetProjects(string rooProjectId, string id)
        {
            List<Project> projects = new List<Project>();
            var projectObj = DynamicGet("/app/rest/projects/id:{0}", id);

            var project = new Project(projectObj.id, projectObj.name, projectObj.href);
            for (int i = 0; i < projectObj.buildTypes.buildType.Length; i++)
            {
                var build = projectObj.buildTypes.buildType[i];
                project.BuildTypes.Add(new BuildType(this, rooProjectId, build.id, build.name, build.href, build.webUrl, project));
            }

            
            projects.Add(project);

            for (int i = 0; i < projectObj.projects.count; i++)
            {
                var childProjectObj = projectObj.projects.project[i];
                var childProjectId = childProjectObj.id;
                projects.AddRange(GetProjects(rooProjectId, childProjectId));
            }
            return projects.ToArray();
        }

        internal AgentStatus[] GetAgents()
        {
            var result = new List<AgentStatus>();

            var agents = DynamicGet("/app/rest/agents?includeUnauthorized=false");

            for (int i = 0; i < agents.agent.Length; i++)
            {
                var agent = agents.agent[i];

                if (agent.name.ToString().ToLower() == "dev29") // TODO: use the agent pool instead of name (can be deom with TC 8.1 using API: http://teamcity.codebetter.com/httpAuth/app/rest/agentPools)
                    continue;

                result.Add(new AgentStatus(this, agent.id, agent.name, agent.href));
            }

            return result.ToArray();
        }

        //public User [] GetInvestigations(string buildTypeId)
        //{
        //    var obj = DynamicGet("/app/rest/buildTypes/id:{0}/investigations", buildTypeId);
            
        //    var users = new List<User>();
        //    for (int i = 0; i < obj.investigation.Length; i++)
        //    {
        //        var responsible = obj.investigation[i].responsible;
        //        string userName = responsible.name;
        //        string href = responsible.href;
        //        users.Add(new User(userName, href));
        //    }
        //    return users.ToArray();
        //}

//        public void GetBuildById(int buildId)
//        {
//            var res = m_Client.GetResponse("/app/rest/changes?locator=build:(id:{0})", buildId);
//            var builds = res.DynamicBody;
//        }

        internal dynamic DynamicGet(string urlPartFormat, params object[] args)
        {
            return DynamicGet(string.Format(urlPartFormat, args));
        }

        internal dynamic DynamicGet(string href)
        {
            var res = GetResponse(href);

            var builds = res.DynamicBody;
            return builds;
        }

        private HttpResponse GetResponse(string href, bool api = true)
        {
            if (api && !href.ToLower().StartsWith("/httpauth"))
                href = "/httpAuth" + href;

            var res = m_Client.GetResponse(href);

            if (res.StatusCode != HttpStatusCode.OK)
                throw new Exception(string.Format("TeamCity API error {0}:\n{1}", res.StatusCode, res.RawText));

            return res;
        }
    }
}