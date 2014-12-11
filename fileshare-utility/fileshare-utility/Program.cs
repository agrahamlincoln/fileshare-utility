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
            CurrentUser = db.InsertGet<user>(new user(Environment.UserName));
            CurrentComputer = db.InsertGet<computer>(new computer(Environment.MachineName));
            UnmappedCount = db.InsertGet<master>(new master("UnmappedCount"));
            #endregion

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
                        server DBserver = db.InsertGet<server>(server.dnslookup(NetCon.ServerName));
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
                        //Skip to next network connection
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

                share currentShare = new share(currentServer, NetCon.ShareName);

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

                shares.Add(currentShare);
            }

            foreach (share fileshare in shares)
            {
                //Locate associated share in MappedList
                NetworkConnection NetCon = mappedDrives.Find(
                    x => x.ServerName.ToUpper() == fileshare.server.hostname.ToUpper() &&
                         x.ShareName.ToUpper() == fileshare.shareName.ToUpper()
                    );

                //create Object to reference this mapping
                mapping map = new mapping(fileshare, CurrentUser, CurrentComputer, NetCon.LocalName, NetCon.UserName);

                map = db.InsertGet<mapping>(map);

                map.date = DateTime.Now.ToString();
            }

            //Save final changes
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
