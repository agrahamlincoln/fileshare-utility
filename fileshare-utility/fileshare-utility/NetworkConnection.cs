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
    /// <summary>
    /// Stores information about a Network Connection, Mimics the structure of Windows WMI queries from root\CIMV2\Win32_NetworkConnection
    /// </summary>
    public class NetworkConnection
    {
        /// <summary>The following block are all used to unmap the drives.
        /// </summary>
        /// <remarks>This code was used from aejw's Network Drive class: build 0015 05/14/2004 aejw.com</remarks>
        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2A(string psName, int piFlags, int pfForce);
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;

        //Class Variables
        public UInt32 AccessMask { get; private set; }      // ListofAccess rights  (ex: 1179785)
        public string Caption { get; private set; }         // Short Description    (ex: 'RESOURCE REMEMBERED')
        public string Comment { get; private set; }         // Comment from Net Provider
        public string ConnectionState { get; private set; } // Current state        (ex: 'Disconnected')
        public string ConnectionType { get; private set; }  // Persistence type     (ex: 'Current Connection')
        public string Description { get; private set; }     // Description of object(ex: Caption - ProviderName);
        public string DisplayType { get; private set; }     // Nework Object        (ex: "Domain"; "Share")
        public DateTime InstallDate { get; private set; }   // Object was installed (ex: '') - Mostly null
        public string LocalName { get; private set; }       // Drive Letter         (ex: 'F:')
        public string Name { get; private set; }            // Name of Connection   (ex: '\\SERVER\share (F:)')
        public bool Persistent { get; private set; }        // Persistence of map   (ex: False)
        public string ProviderName { get; private set; }    // Provider of resource (ex: 'Microsoft Windows Network');
        public string RemoteName { get; private set; }      // Remote Name of Share (ex: '\\SERVER\share')
        public string RemotePath { get; private set; }      // Full Path to resource(ex: '\\SERVER\share')
        public string ResourceType { get; private set; }    // Type of resource     (ex: 'Disk'; 'Print')
        public string Status { get; private set; }          // Current status       (ex: 'OK'; 'Unknown')
        public string UserName { get; private set; }        // Username of Mapping  (ex: 'DOMAIN.LOCAL\username')

        #region Constructors
        /// <summary>No-Arg constructor
        /// </summary>
        public NetworkConnection()
            : this(0, "", "", "", "",
                "", "", DateTime.Now, "", "",
                false, "", "", "", "", "", "") { }

        /// <summary>
        /// Constructor with Drive Letter and Share Path
        /// </summary>
        public NetworkConnection(
            string LocalName,
            string RemotePath,
            string UserName)
            : this (0, "", "", "", "",
                    "", "", DateTime.Now, LocalName, LocalName + "(" + RemotePath + ")", 
                    false, "", RemotePath, RemotePath, "", "OK", UserName) { }

        public NetworkConnection(NetworkConnection NetCon)
        {
            this.AccessMask =       NetCon.AccessMask;
            this.Caption =          NetCon.Caption;
            this.Comment =          NetCon.Comment;
            this.ConnectionState =  NetCon.ConnectionState;
            this.ConnectionType =   NetCon.ConnectionType;
            this.Description = NetCon.Description;
            this.DisplayType = NetCon.DisplayType;
            this.InstallDate = NetCon.InstallDate;
            this.LocalName = NetCon.LocalName;
            this.Name = NetCon.Name;
            this.Persistent = NetCon.Persistent;
            this.ProviderName = NetCon.ProviderName;
            this.RemoteName = NetCon.RemoteName;
            this.ResourceType = NetCon.ResourceType;
            this.Status = NetCon.Status;
            this.UserName = NetCon.UserName;
        }

        /// <summary>
        /// Constructor with all Arguments
        /// </summary>
        public NetworkConnection(
            UInt32 AccessMask,
            string Caption,
            string Comment,
            string ConnectionState,
            string ConnectionType,
            string Description,
            string DisplayType,
            DateTime InstallDate,
            string LocalName,
            string Name,
            bool Persistent,
            string ProviderName,
            string RemoteName,
            string RemotePath,
            string ResourceType,
            string Status,
            string UserName)
        {
            this.AccessMask = AccessMask;
            this.Caption = Caption;
            this.Comment = Comment;
            this.ConnectionState = ConnectionState;
            this.ConnectionType = ConnectionType;
            this.Description = Description;
            this.DisplayType = DisplayType;
            this.InstallDate = InstallDate;
            this.LocalName = LocalName;
            this.Name = Name;
            this.Persistent = Persistent;
            this.ProviderName = ProviderName;
            this.RemoteName = RemoteName;
            this.RemotePath = RemotePath;
            this.ResourceType = ResourceType;
            this.Status = Status;
            this.UserName = UserName;
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

                //Enumerate all network drives and store in ArrayList object.
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    //get information using WMI
                    UInt32 AccessMask =         Convert.ToUInt32(queryObj["AccessMask"]);
                    string Caption =            String.Format("{0}", queryObj["Caption"]);
                    string Comment =            String.Format("{0}", queryObj["Comment"]);
                    string ConnectionState =    String.Format("{0}", queryObj["ConnectionState"]);
                    string ConnectionType =     String.Format("{0}", queryObj["ConnectionType"]);
                    string Description =        String.Format("{0}", queryObj["Description"]);
                    string DisplayType =        String.Format("{0}", queryObj["DisplayType"]);
                    DateTime InstallDate =      Convert.ToDateTime(queryObj["InstallDate"]);
                    string LocalName =          String.Format("{0}", queryObj["LocalName"]);
                    string Name =               String.Format("{0}", queryObj["Name"]);
                    Boolean Persistent =        Convert.ToBoolean(queryObj["Persistent"]);
                    string ProviderName =       String.Format("{0}", queryObj["ProviderName"]);
                    string RemoteName =         String.Format("{0}", queryObj["RemoteName"]);
                    string RemotePath =         String.Format("{0}", queryObj["RemotePath"]);
                    string ResourceType =       String.Format("{0}", queryObj["ResourceType"]);
                    string Status =             String.Format("{0}", queryObj["Status"]);
                    string UserName =           String.Format("{0}", queryObj["UserName"]);

                    if (driveLetter.IsMatch(LocalName))
                    {
                        drivesFromWMI.Add(new NetworkConnection(
                            AccessMask, Caption, Comment, ConnectionState, ConnectionType, Description,
                            DisplayType, InstallDate, LocalName, Name, Persistent, ProviderName, RemoteName,
                            RemotePath, ResourceType, Status, UserName
                            ));
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
        public override string ToString()
        {
            return Name;
        }

        public string GetServer()
        {
            var arry = RemotePath.Split('\\');
            return arry[2];
        }

        public string GetShareName()
        {
            var arry = RemotePath.Split('\\');
            return arry[3];
        }
    }
}
