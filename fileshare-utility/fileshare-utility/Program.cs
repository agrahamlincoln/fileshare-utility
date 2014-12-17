using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ExtensionMethods;

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
            user CurrentUser;                       // User of person executing app
            computer CurrentComputer;               // Computer executing app
            master UnmappedCount;                   // Number of fileshares unmapped
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
                mappedDrives = NetworkConnection.ListCurrentlyMappedDrives().DNSable();
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

            // Initialize Program Variables
            CurrentUser = db.FindOrInsert<user>(new user(Environment.UserName));
            CurrentComputer = db.FindOrInsert<computer>(new computer(Environment.MachineName));
            UnmappedCount = db.FindOrInsert<master>(new master("UnmappedCount"));
            #endregion

            logger.Write("=== List of Currently Mapped Drives ===");

            foreach (NetworkConnection NetCon in mappedDrives)
            {
                logger.Write("Now Processing: " + NetCon.ToString());

                MappedShare MapShare = new MappedShare(NetCon);

                //Add the server
                server currentServer = db.FindOrInsert<server>(server.dnslookup(NetCon.GetServer()));
                //Update the date of the server
                currentServer.date = DateTime.Now.ToString();

                if (!currentServer.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    logger.Write("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                MapShare.mapping.share = new share(currentServer, NetCon.GetShareName());

                if (db.Get<share>(MapShare.mapping.share) == null)
                {
                    MapShare.mapping.share = db.FindOrInsert<share>(MapShare.mapping.share);
                }
                else if (!MapShare.mapping.share.server.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    logger.Write("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                MapShare.mapping = db.FindOrInsert<mapping>(MapShare.mapping);
                MapShare.mapping.date = DateTime.Now.ToString();
            }

            //Save final changes
            db.SaveChanges();
        }
    }
}
