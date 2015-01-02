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
            FileWriter FileLogger;                  // Run-Time log
            List<NetworkConnection> mappedDrives;   // List of Mapped Network Drives
            FileLocations locations;                // Stores all FilePath and Directory locations

            // Entity Framework Objects
            DatabaseService db;                     // Database Operator

            // Database Entity Objects
            user CurrentUser;                       // User of person executing app
            computer CurrentComputer;               // Computer executing app
            master UnmappedCount;                   // Number of fileshares unmapped
            #endregion

            #region Initialization
            locations = new FileLocations();

            // Logger Initialization
            FileLogger = new FileWriter();
            FileLogger.Header();
            FileLogger.Output("Global Log Path: " + locations.logDir);

            // Get Mapped Drives
            mappedDrives = new List<NetworkConnection>();
            try
            {
                mappedDrives = NetworkConnection.ListCurrentlyMappedDrives().DNSable();
            }
            catch (ManagementException crap)
            {
                FileLogger.Output(crap.ToString());
                Environment.Exit(0);
            }

            // Database Initialization
            db = new DatabaseService(locations.dbDir);
            if (!File.Exists(locations.dbPath))
                db.InitOutput();

            // Initialize Program Variables
            CurrentUser = db.FindOrInsert<user>(new user(Environment.UserName));
            CurrentComputer = db.FindOrInsert<computer>(new computer(Environment.MachineName));
            UnmappedCount = db.FindOrInsert<master>(new master("UnmappedCount"));
            #endregion

            FileLogger.Output("=== List of Currently Mapped Drives ===");

            foreach (NetworkConnection NetCon in mappedDrives)
            {
                server CurrentServer;
                share CurrentShare;
                mapping CurrentMapping;

                FileLogger.Output("Now Processing: " + NetCon.ToString());

                //### PROCESS THE SERVER ###
                //Add the server
                CurrentServer = db.FindOrInsert<server>(server.dnslookup(NetCon.GetServer()));
                //Update the date of the server
                CurrentServer.date = DateTime.Now.ToString();

                if (!CurrentServer.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    FileLogger.Output("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                //### PROCESS THE SHARE ###
                CurrentShare = db.FindOrInsert<share>(new share(CurrentServer, NetCon.GetShareName()));

                if (!CurrentShare.server.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    FileLogger.Output("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                //### PROCESS THE MAPPING ###
                //All the required entities are created and verified
                CurrentMapping = new mapping(CurrentShare, CurrentUser, CurrentComputer, NetCon.LocalName, NetCon.UserName);

                CurrentMapping = db.FindOrInsert<mapping>(CurrentMapping);
                CurrentMapping.date = DateTime.Now.ToString();
            }

            //Save final changes
            db.SaveChanges();
        }
    }
}
