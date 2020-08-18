using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Tc.Monitor.Engine;
using Tc.Monitor.Server.DbManagerLib;

namespace Tc.Monitor.Server.HostServerManagerLib
{
    public class ValuesController : ApiController
    {

        private DbManager dbManager = new DbManager();
        // GET api/values 
//        public List<BuildType> Get()
//        {
//            return dbManager.GetFailedBuilds();
//            //return new string[] { "value1", "value2" };
//            //return getErrorList(); //returns the errorsList from Startup.
//        }



        // GET api/values/5 
//        public BuildType Get(int id)
//        {
//            return dbManager.GetFailedBuild(id);
//        }

        //// POST api/values 
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/values/5 
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/values/5 
        //public void Delete(int id)
        //{
        //}
    }
}
