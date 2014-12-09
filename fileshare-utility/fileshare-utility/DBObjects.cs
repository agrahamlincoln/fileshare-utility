using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{

    partial class computer
    {
        public computer(string hostname)
            : this()
        {
            this.hostname = hostname;
        }
    }

    partial class server
    {
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="srvr">server object to copy</param>
        public server(server srvr)
        {
            this.serverID = srvr.serverID;
            this.hostname = srvr.hostname;
            this.domain = srvr.domain;
            this.active = srvr.active;
            this.date = srvr.date;
            this.shares = srvr.shares;
        }

        public server(string hostname, string domain)
            : this()
        {
            this.hostname = hostname;
            this.domain = domain;
            this.active = true;
            this.date = DateTime.Now.ToString();
        }


        /// <summary>
        /// Performs a DNS lookup on an address and returns a server object
        /// </summary>
        /// <param name="address">IP Address/Hostname of a server</param>
        /// <returns>null if not resolved; server object representing the resolved host.</returns>
        /// <exception cref="ArgumentNullException">Argument is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Argument is more than 255 chars</exception>
        /// <exception cref="SocketException">Host was not resolvable</exception>
        /// <exception cref="ArgumentException">Invalid IP Address</exception>
        public static server dnslookup(string address)
        {
            server resolved = null;

            IPHostEntry hostEntry;
            hostEntry = Dns.GetHostEntry(address);

            var host = hostEntry.HostName.Split('.').ToList<string>();

            var name = host.ElementAt(0);
            host.RemoveAt(0);
            var domain = String.Join(".", host.ToArray());

            resolved = new server(name, domain);

            return resolved;
        }
    }

    partial class share
    {
        public share(server currServer, string shareName)
            : this()
        {
            this.serverID = currServer.serverID;
            this.shareName = shareName;
            this.server = currServer;
            this.active = true;
        }
    }

    partial class user
    {
        public user(string username)
            : this()
        {
            this.username = username;
        }
    }

    partial class mapping
    {
        /// <summary>
        /// No-Arg constructor, initializes values to un-real
        /// </summary>
        public mapping()
        {
            this.shareID = -1;
            this.computerID = -1;
            this.userID = -1;
            this.letter = "C";
            this.username = "";
            this.date = DateTime.MinValue.ToString();
        }

        public mapping(share fileshare, user usr, computer comp, string letter, string username)
        {
            this.shareID = fileshare.shareID;
            this.userID = usr.userID;
            this.computerID = comp.computerID;
            this.letter = letter;
            this.username = username;
            this.date = DateTime.Now.ToString();
        }
    }
}
