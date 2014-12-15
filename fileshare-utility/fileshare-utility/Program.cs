using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Instantiation
            // Program Variables
            LogWriter logger;                       // Run-Time log
            LogWriter globalLog;                    // Unmapped/Discovered Drive log
            List<NetworkConnection> mappedDrives;   // List of Mapped Network Drives
            FileLocations locations;                // Stores all FilePath and Directory locations

            // Entity Framework Objects
            DataContext db;                         // Entity Framework Context

            // Database Entity Objects
            user CurrentUser;                       //User of person executing app
            computer CurrentComputer;               //Computer executing app
            master UnmappedCount;                   //Number of fileshares unmapped
            #endregion

            #region Initialization
            locations = new FileLocations();

            // Logger Initialization
            logger = new LogWriter();
            globalLog = new LogWriter("Log.txt");

            globalLog.logPath = locations.logDir;
            logger.header();
            logger.Write("Global Log Path: " + locations.logDir);

            // Get Mapped Drives
            mappedDrives = new List<NetworkConnection>();
            try
            {
                mappedDrives = NetworkConnection.ListCurrentlyMappedDrives();
            }
            catch (ManagementException crap)
            {
                logger.Write(crap.ToString());
                Environment.Exit(0);
            }

            // Database Initialization
            db = new DataContext(locations.dbDir);
            if (!File.Exists(locations.dbPath))
                db.BuildDB();

            //Initialize Program Variables
            CurrentUser = db.InsertGet<user>(new user(Environment.UserName));
            CurrentComputer = db.InsertGet<computer>(new computer(Environment.MachineName));
            UnmappedCount = db.InsertGet<master>(new master("UnmappedCount"));
            #endregion

            logger.Write("=== List of Currently Mapped Drives ===");
            List<server> servers = new List<server>();
            List<share> ActiveShares = new List<share>();
            foreach (NetworkConnection NetCon in mappedDrives)
            {
                logger.Write("Now Processing: " + NetCon.ToString());

                server currentServer = servers.FirstOrDefault(
                    x => x.hostname.ToUpper() == NetCon.GetServer().ToUpper()
                    );

                if (currentServer == null)
                {
                    //server is not in current list.
                    try
                    {
                        //Create temp object and verify hostname/domain through DNS
                        var dnsServer = server.dnslookup(NetCon.GetServer());

                        //Add the temp object to the DB
                        server DBserver = db.InsertGet<server>(dnsServer);

                        //Add the DB Object to the list
                        servers.Add(DBserver);

                        //Set the date on the DB object to NOW
                        DBserver.date = DateTime.Now.ToString();
                    }
                    catch (SocketException)
                    {
                        logger.Write("Could not resolve hostname: " + NetCon.GetServer());
                        //Skip to next network connection
                        continue;
                    }
                }

                currentServer = servers.FirstOrDefault(
                    x => x.hostname.ToUpper() == NetCon.GetServer().ToUpper()
                    );

                if (!currentServer.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    logger.Write("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                share currentShare = new share(currentServer, NetCon.GetShareName());

                if (db.Get<share>(currentShare) == null)
                {
                    currentShare = db.InsertGet<share>(currentShare);
                }
                else if (!currentShare.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    logger.Write("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                ActiveShares.Add(currentShare);
            }

            foreach (share fileshare in ActiveShares)
            {
                //Locate associated share in MappedList
                NetworkConnection NetCon = mappedDrives.Find(
                    x => x.GetServer().ToUpper() == fileshare.server.hostname.ToUpper() &&
                         x.GetShareName().ToUpper() == fileshare.shareName.ToUpper()
                    );

                //create Object to reference this mapping
                mapping map = new mapping(fileshare, CurrentUser, CurrentComputer, NetCon.LocalName, NetCon.UserName);

                map = db.InsertGet<mapping>(map);

                map.date = DateTime.Now.ToString();
            }

            //Save final changes
            db.SaveChanges();
        }
    }
}
