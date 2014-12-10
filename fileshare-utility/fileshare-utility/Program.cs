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
        private const string LOG_SUBDIRECTORY = "\\logs";
        private const string DATA_SUBDIRECTORY = "\\data";

        static void Main(string[] args)
        {
            #region Instantiation
            // Program Variables
            LogWriter logger;                       // Run-Time log
            LogWriter globalLog;                    // Unmapped/Discovered Drive log
            string runningPath;                     // Path of executed application
            List<NetworkConnection> mappedDrives;   // List of Mapped Network Drives

            // Entity Framework Objects
            DataContext db;                         // Entity Framework Context

            // Database Entity Objects
            user CurrentUser;                       //User of person executing app
            computer CurrentComputer;               //Computer executing app
            master UnmappedCount;                   //Number of fileshares unmapped
            #endregion

            #region Initialization
            // Directory Initialization
            runningPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            CreateSubdirectoryIfNoExists("logs");
            CreateSubdirectoryIfNoExists("data");
            AppDomain.CurrentDomain.SetData("DataDirectory", runningPath);

            // Logger Initialization
            logger = new LogWriter();
            globalLog = new LogWriter("Log.txt");

            globalLog.logPath = runningPath + LOG_SUBDIRECTORY;
            logger.header();
            logger.Write("Global Log Path: " + globalLog.logPath);

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
            db = new DataContext(runningPath + LOG_SUBDIRECTORY);
            if (!File.Exists(AppDomain.CurrentDomain.GetData("DataDirectory") + DATA_SUBDIRECTORY + "\\fileshare-utility.s3db"))
                db.BuildDB();

            //Initialize Program Variables
            CurrentUser = db.getUser(Environment.UserName);
            CurrentComputer = db.getComputer(Environment.MachineName);
            UnmappedCount = db.getMaster("UnmappedCount");
            #endregion

            //Add new user if not found
            if (CurrentUser == null)
            {
                db.Insert<user>(new user(Environment.UserName));
                CurrentUser = db.getUser(Environment.UserName);
            }

            //Add new computer if not found
            if (CurrentComputer == null)
            {
                db.Insert<computer>(new computer(Environment.MachineName));
                CurrentComputer = db.getComputer(Environment.MachineName);
            }

            if (UnmappedCount == null)
            {
                db.Insert<master>(new master("UnmappedCount"));
                UnmappedCount = db.getMaster("UnmappedCount");
            }

            // Make sure all shares are added to the DB
            logger.Write("=== List of Currently Mapped Drives ===");
            List<server> servers = new List<server>();
            List<share> shares = new List<share>();
            foreach (NetworkConnection NetCon in mappedDrives)
            {
                logger.Write("Now Processing: " + NetCon.ToString());

                if (!servers.Exists(
                    x => x.hostname.ToUpper() == NetCon.ServerName.ToUpper()
                    ))
                {
                    //server is not in current list.
                    try
                    {
                        server DNSserver = server.dnslookup(NetCon.ServerName);

                        //query database and add if necessary.
                        server DBserver = db.getServer(DNSserver.hostname, DNSserver.domain);

                        if (DBserver == null)
                        {
                            //server was not found; add to Database.
                            db.Insert<server>(DNSserver);
                            DBserver = db.getServer(DNSserver.hostname, DNSserver.domain);
                        }
                        servers.Add(DBserver);

                        DBserver.date = DateTime.Now.ToString();
                    }
                    catch (SocketException)
                    {
                        logger.Write("Could not resolve hostname: " + NetCon.ServerName);
                        //Skip to next network connection
                        continue;
                    }
                    catch (Exception crap)
                    {
                        logger.Write("An unexpected error occurred: " + crap.ToString());
                        continue;
                    }
                }
                
                server currentServer = servers.FirstOrDefault(
                    x => x.hostname.Equals(NetCon.ServerName, StringComparison.OrdinalIgnoreCase)
                );

                if (!currentServer.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    logger.Write("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                share fileshare = db.getShare(currentServer.serverID, NetCon.ShareName);

                if (fileshare == null)
                {
                    fileshare = new share(currentServer, NetCon.ShareName);
                    db.Insert<share>(fileshare);
                    fileshare = db.getShare(fileshare.serverID, fileshare.shareName);
                }
                else if (!fileshare.active)
                {
                    NetCon.unmap();
                    UnmappedCount.increment();
                    logger.Write("Unmapping Fileshare: " + NetCon.ToString());
                    continue;
                }

                shares.Add(fileshare);
            }

            List<mapping> mappedList = new List<mapping>();
            //Query the DB and get all mappings
            //all mappings of the current user and computer
            IQueryable<mapping> dbMappings = db.mappings.Where<mapping>(
            x => x.computerID == CurrentComputer.computerID &&
                 x.userID == CurrentUser.userID
            );

            foreach (share fileshare in shares)
            {
                mapping map = dbMappings.FirstOrDefault<mapping>(x => x.shareID == fileshare.shareID);

                if (map == null)
                {
                    NetworkConnection NetCon = mappedDrives.Find(
                        x => x.ServerName.ToUpper() == fileshare.server.hostname.ToUpper() &&
                             x.ShareName.ToUpper() == fileshare.shareName.ToUpper()
                        );
                    map = new mapping(fileshare, CurrentUser, CurrentComputer, NetCon.LocalName, NetCon.UserName);

                    db.Insert<mapping>(map);
                    map = db.getMapping(map.shareID, map.userID, map.computerID);
                    logger.Write("Added new Mapping to [mappings]: " + map.ToString());
                }

                map.date = DateTime.Now.ToString();
                mappedList.Add(map);
            }

            db.SaveChanges();
        }

        /// <summary>
        /// Creates a Subdirectory under the running program if none exists
        /// </summary>
        /// <param name="name">name of the subdirectory to create</param>
        public static void CreateSubdirectoryIfNoExists(string name)
        {
            string runningPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(runningPath + "\\" + name))
                Directory.CreateDirectory(runningPath + "\\" + name);
        }
    }
}
