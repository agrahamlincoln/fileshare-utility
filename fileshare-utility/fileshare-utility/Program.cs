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
            List<NetworkConnection> AllMappedDrives;   // List of Mapped Network Drives
            List<NetworkConnection> ResolvedMappedDrives; // List of all Mapped Network Drives where the server is Resolvable
            List<server> DistinctServers;           // List of all distinct servers
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
            DistinctServers = new List<server>();

            // Logger Initialization
            FileLogger = new FileWriter();
            FileLogger.Output(FileLogger.Header());
            FileLogger.Output("Global Log Path: " + locations.logDir);

            // Get Mapped Drives
            try
            {
                AllMappedDrives = NetworkConnection.ListCurrentlyMappedDrives();
            }
            catch (ManagementException crap)
            {
                FileLogger.Output(crap.ToString());
                return;
            }
            ResolvedMappedDrives = AllMappedDrives.DNSable();

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

            //Verify that a server mapped is not marked as inactive
            foreach (NetworkConnection NetCon in AllMappedDrives.Except(ResolvedMappedDrives))
            {
                server CurrentServer;

                try
                {
                    //Look in the local list first
                    CurrentServer = DistinctServers.First(x => x.name == NetCon.GetServerHostname().ToUpper());
                    VerifyServerAndUnmapInactives(CurrentServer.active, NetCon, ref UnmappedCount, FileLogger);
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        //Look in the Database
                        CurrentServer = db.FindServer(NetCon.GetServerHostname());
                        VerifyServerAndUnmapInactives(CurrentServer.active, NetCon, ref UnmappedCount, FileLogger);

                        //Add this server to the local list of servers
                        DistinctServers.Add(CurrentServer);
                    }
                    catch (KeyNotFoundException)
                    {
                        //Server is not in the DB
                        FileLogger.Output("Mapping with Non-Resolvable, Unknown server found: " + NetCon.ToString());
                        continue;
                    }
                }
            }

            foreach (NetworkConnection NetCon in ResolvedMappedDrives)
            {
                server CurrentServer;
                share CurrentShare;
                mapping CurrentMapping;
                Tuple<string, string> HostInfo;

                FileLogger.Output("Now Processing: " + NetCon.ToString());

                //### PROCESS THE SERVER ###
                //Add the server
                using (DNSService dns = new DNSService())
                {
                    HostInfo = dns.lookup(NetCon.GetServerHostname());
                    CurrentServer = db.FindOrInsert<server>(new server(HostInfo.Item1, HostInfo.Item2));
                    //Update the date of the server
                    CurrentServer.date = DateTime.Now.ToString();

                    VerifyServerAndUnmapInactives(CurrentServer.active, NetCon, ref UnmappedCount, FileLogger);
                    if (NetCon.Unmapped)
                        continue;
                }

                //### PROCESS THE SHARE ###
                CurrentShare = db.FindOrInsert<share>(new share(CurrentServer, NetCon.GetShareName()));

                VerifyServerAndUnmapInactives(CurrentShare.active, NetCon, ref UnmappedCount, FileLogger);
                if (NetCon.Unmapped)
                    continue;   

                //### PROCESS THE MAPPING ###
                //All the required entities are created and verified
                CurrentMapping = new mapping(CurrentShare, CurrentUser, CurrentComputer, NetCon.LocalName, NetCon.UserName);

                CurrentMapping = db.FindOrInsert<mapping>(CurrentMapping);
                CurrentMapping.date = DateTime.Now.ToString();
            }

            //Save final changes
            db.SaveChanges();
        }

        private static void VerifyServerAndUnmapInactives(bool Active, NetworkConnection NetCon, ref master UnmappedCount, IWriter output)
        {
            if (!Active)
            {
                NetCon.unmap();
                UnmappedCount.increment();
                output.Output("Unmapping Fileshare: " + NetCon.ToString());
            }
        }
    }
}
