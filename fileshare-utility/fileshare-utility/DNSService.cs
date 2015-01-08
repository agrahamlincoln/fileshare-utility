using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    /// <summary>
    /// DNSService class will perform DNS Lookups and will Cache the results for faster access and less network footprint
    /// </summary>
    class DNSService : IDisposable
    {
        public bool disposed = false;  // Whether the object has been disposed or not
        static List<IPHostEntry> ResolvedHosts = new List<IPHostEntry>();            // Local storage of resolved hosts
        static Dictionary<string, string> HostsAndDomains = new Dictionary<string, string>(); // Local storage of resolved hosts and domains
        static Dictionary<string, bool> AttemptedLookups = new Dictionary<string, bool>();  // Historical list of attempted address lookups and the result

        /// <summary>
        /// Performs a DNS lookup on an address and returns a server object
        /// </summary>
        /// <param name="address">IP Address/Hostname of a server</param>
        /// <returns>KeyValuePair with the Hostname and the Domain</returns>
        /// <exception cref="InvalidOperationException">DNS Lookup Failed to Resolve the address</exception>
        public Tuple<string, string> lookup(string address)
        {
            Tuple<string, string> Result;

            //Verify that the DNS Lookup has not been performed before
            if (AttemptedLookups.ContainsKey(address))
            {
                KeyValuePair<string, bool> PreviousAttempt =
                    AttemptedLookups.First(x => x.Key.Equals(address, StringComparison.OrdinalIgnoreCase));

                if (PreviousAttempt.Value)
                {
                    KeyValuePair<string, string> HostAndDomain =
                        HostsAndDomains.First(x => x.Key.Equals(address, StringComparison.OrdinalIgnoreCase));
                    Result = new Tuple<string, string>(HostAndDomain.Key, HostAndDomain.Value);
                }
                else
                {
                    throw new InvalidOperationException("A DNS Lookup previously failed to resolve this address: " + address);
                }
            }
            else
            {
                //Perform a new DNS Lookup on this host
                IPHostEntry hostEntry;

                try
                {
                    hostEntry = Dns.GetHostEntry(address.ToUpper());
                    ResolvedHosts.Add(hostEntry);

                    HostsAndDomains.Add(GetHostname(hostEntry), GetDomain(hostEntry));
                    Result = new Tuple<string, string>(GetHostname(hostEntry), GetDomain(hostEntry));

                    AttemptedLookups.Add(GetHostname(hostEntry), true);
                }
                catch (SocketException)
                {
                    AttemptedLookups.Add(address, false);
                    throw new InvalidOperationException("A DNS Lookup failed to resolve this address: " + address);
                }
            }
            return Result;
        }

        /// <summary>
        /// Peforms a DNS Lookup on multiple addresses.
        /// </summary>
        /// <param name="addresses">List of hostnames/ip addresses to lookup</param>
        /// <returns>Tuples with the Hostname and Domain of the resolved host</returns>
        public List<Tuple<string, string>> MultiLookup(List<string> addresses)
        {
            List<Tuple<string, string>> results = new List<Tuple<string, string>>();

            foreach (string address in addresses)
            {
                try
                {
                    results.Add(lookup(address));
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
            }

            return results;
        }

        private static string GetHostname(IPHostEntry Host)
        {
            return Host.HostName.Split('.').ElementAt(0);
        }

        private static string GetDomain(IPHostEntry Host)
        {
            var host = Host.HostName.Split('.').ToList<string>();
            host.RemoveAt(0);
            return String.Join(".", host.ToArray());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                //Free any managed objects
            }
            //Free any unmanaged objects

            disposed = true;
        }
    }
}
