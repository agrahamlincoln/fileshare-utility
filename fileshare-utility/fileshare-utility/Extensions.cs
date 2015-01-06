using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fileshare_utility;
using System.Net;
using System.Net.Sockets;

namespace ExtensionMethods
{
    public static class Extensions
    {
        public static List<string> DistinctServers(this List<fileshare_utility.NetworkConnection> NetConList)
        {
             List<string> distinctServers = NetConList
                .GroupBy(x => x.GetServerHostname())
                .Select(y => y.First())
                .Select(z => z.GetServerHostname())
                .ToList();

             return distinctServers;
        }

        public static List<NetworkConnection> DNSable(this List<fileshare_utility.NetworkConnection> NetConList)
        {
            List<string> distinctServers = NetConList.DistinctServers();
            List<string> resolvedServers = new List<string>();

            foreach (string hostname in distinctServers)
            {
                try
                {
                    Dns.GetHostEntry(hostname.ToUpper());
                    resolvedServers.Add(hostname);
                }
                catch (SocketException)
                {
                    //host did not resolve
                    continue;
                }
            }

            return NetConList.Where(
                x => resolvedServers.Contains(x.GetServerHostname().ToUpper())
                )
                .ToList();
        }
    }
}
