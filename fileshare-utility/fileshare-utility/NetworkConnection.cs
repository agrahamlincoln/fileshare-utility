using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fileshare_utility
{
    /// <summary>Stores information about a Network Connection, Mimics the structure of Windows WMI queries from root\CIMV2\Win32_NetworkConnection
    /// </summary>
    sealed public class NetworkConnection
    {
        /// <summary>The following block are all used to unmap the drives.
        /// </summary>
        /// <remarks>This code was used from aejw's Network Drive class: build 0015 05/14/2004 aejw.com</remarks>
        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2A(string psName, int piFlags, int pfForce);
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;

        //Class Variables
        public string LocalName { get; set; }
        public string Domain { get; set; }
        public string UserName { get; set; }
        public bool Persistent { get; set; }
        public string ServerName { get; set; }
        public string ShareName { get; set; }

        #region Constructors
        /// <summary>No-Arg constructor
        /// </summary>
        public NetworkConnection() : this("", "", "") { }

        /// <summary>Constructor with Drive Letter and Share Path
        /// </summary>
        /// <param name="LocalName">Local Drive Letter/Local Name of the Network Drive Mapping</param>
        /// <param name="RemoteName">Full Path of the Network Drive</param>
        public NetworkConnection(string LocalName, string ServerName, string ShareName) : this(LocalName, ServerName, ShareName, "", "", false) { }

        /// <summary>Constructor with all Arguments
        /// </summary>
        /// <param name="LocalName">Local Drive Letter/Local Name of the Network Drive Mapping</param>
        /// <param name="RemoteName">Full Path of the Network Drive</param>
        /// <param name="domain">Domain of the user associated to this mapping</param>
        /// <param name="UserName">Username of the user associated to this mapping</param>
        /// <param name="Persistent">Drive Mapping persistence</param>
        public NetworkConnection(string LocalName, string ServerName, string ShareName,string domain, string UserName, bool Persistent)
        {
            this.LocalName = LocalName;
            this.ServerName = ServerName;
            this.ShareName = ShareName;
            this.Domain = domain;
            this.UserName = UserName;
            this.Persistent = Persistent;
        }
        #endregion

        /// <summary>Creates a list of Network Connections from WMI
        /// </summary>
        /// <returns>List of Network Connections from WMI</returns>
        /// <exception cref="System.Management.ManagementException">Thrown when WMI query fails.</exception>
        internal static List<NetworkConnection> ListCurrentlyMappedDrives()
        {
            List<NetworkConnection> drivesFromWMI = new List<NetworkConnection>();

            Regex driveLetter = new Regex("^[A-z]:");

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2",
                                                                                 "SELECT * FROM Win32_NetworkConnection");

                string LocalName;
                string ServerName;
                string ShareName;
                string[] QualifiedUserName;
                bool Persistent;

                string RemoteName;

                //Enumerate all network drives and store in ArrayList object.
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    //get information using WMI
                    LocalName = String.Format("{0}", queryObj["LocalName"]);
                    Persistent = Boolean.Parse(String.Format("{0}", queryObj["Persistent"]));
                    RemoteName = String.Format("{0}", queryObj["RemoteName"]);
                    QualifiedUserName = String.Format("{0}", queryObj["UserName"]).Split('\\');

                    ServerName = RemoteName.Split('\\')[2];
                    ShareName = RemoteName.Split('\\')[3];

                    if (driveLetter.IsMatch(LocalName))
                    {
                        if (QualifiedUserName.Length >= 2)
                            drivesFromWMI.Add(new NetworkConnection(LocalName, ServerName, ShareName, QualifiedUserName[0], QualifiedUserName[1], Persistent));
                        else
                            drivesFromWMI.Add(new NetworkConnection(LocalName, ServerName, ShareName, "", "", Persistent));
                    }
                }
            }
            catch (ManagementException e)
            {
                throw new ManagementException("An error occurred while querying for WMI data.\nCall Stack: " + e.Message);
            }

            return drivesFromWMI;
        }

        /// <summary>
        /// Unmaps the drive using core windows API's
        /// </summary>
        /// <remarks>This code was used from aejw's Network Drive class: build 0015 05/14/2004 aejw.com</remarks>
        internal void unmap()
        {
            bool force = false;
            //call unmap and return
            int iFlags = 0;
            if (Persistent) { iFlags += CONNECT_UPDATE_PROFILE; }
            int i = WNetCancelConnection2A(LocalName, iFlags, Convert.ToInt32(force));
            if (i > 0) { throw new System.ComponentModel.Win32Exception(i); }
        }

        /// <summary>To String method.
        /// </summary>
        /// <returns>Local Drive Letter + Full UNC Path</returns>
        internal string ToString()
        {
            string str;
            str = LocalName + "\t" + getRemoteName() + "\tDomain: " + Domain;
            return str;
        }

        /// <summary>
        /// Formats the server name and share name into a full fileshare path
        /// </summary>
        /// <returns>Full fileshare path</returns>
        internal string getRemoteName()
        {
            return "\\\\" + ServerName + "\\" + ShareName;
        }
    }
}
