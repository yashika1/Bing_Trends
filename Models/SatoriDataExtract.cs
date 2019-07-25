using SatoriBrowser.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Hackathon.Models
{
    public class SatoriDataExtract
    {
        public IList<Tuple<string, string, string>> ExtractFileData()
        {
            var script = new IScope.Script(
                "SELECT DominantType, Name, EntityCount FROM (SSTREAM \"{0}\") ;",
                "/local/users/ruaneja/Hackathon/30DaysTopEntityInDomain.ss?property=info");
            List<List<Object>> result = IScope.ExecuteScript(script);
            List<Tuple<string, string, string>> graph = new List<Tuple<string, string, string>>();
            foreach (List<Object> entry in result)
            {
                if (entry[0] == null || entry[1] == null || entry[2] == null)
                    continue;
                graph.Add(new Tuple<string, string, string>(entry[0].ToString(), entry[1].ToString(), entry[2].ToString()));
            }
            return graph;
        }
    }
}
