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
            //Program Variables
            LogWriter logger = new LogWriter();                                     // Run-Time log
            LogWriter globalLog = new LogWriter("Log.txt");                         // Unmapped/Discovered Drive log
            string runningPath;                                                     // Path of executed application
            List<NetworkConnection> mappedDrives = new List<NetworkConnection>();   // List of Mapped Network Drives

            //Entity Framework Objects
            DataContext db = new DataContext();                                     // Entity Framework Context
            DataOperator dataOper = new DataOperator(db);                           // For Mutators and Accessors

            //Database Entity Objects
            user CurrentUser;           //User of person executing app
            computer CurrentComputer;   //Computer executing app

            //Initialize Program Variables
            runningPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            globalLog.logPath = runningPath + "\\logs";
            logger.header();
            logger.Write("logpath: " + globalLog.logPath);
            CurrentUser = dataOper.getUser(Environment.UserName);
            CurrentComputer = dataOper.getComputer(Environment.MachineName);

            //Add new user if not found
            if (CurrentUser == null)
            {
                dataOper.Insert<user>(new user(Environment.UserName));
                globalLog.Write("Added new user to [users]: " + Environment.UserName);
                CurrentUser = dataOper.getUser(Environment.UserName);
            }

            //Add new computer if not found
            if (CurrentComputer == null)
            {
                dataOper.Insert<computer>(new computer(Environment.MachineName));
                globalLog.Write("Added new computer to [computers]: " + Environment.MachineName);
                CurrentComputer = dataOper.getComputer(Environment.MachineName);
            }

            //Get List of Network Connections from WMI
            try
            {
                mappedDrives = NetworkConnection.ListCurrentlyMappedDrives();
            }
            catch (ManagementException crap)
            {
                logger.Write(crap.ToString());
                Environment.Exit(0);
            }

            // Make sure all shares are added to the DB
            logger.Write("=== List of Currently Mapped Drives ===");
            List<server> servers = new List<server>();
            List<share> shares = new List<share>();
            foreach (NetworkConnection NetCon in mappedDrives)
            {
                logger.Write("Now Processing: " + NetCon.ToString());

                if (!servers.Exists(
                    x => x.hostname.Equals(NetCon.ServerName, StringComparison.OrdinalIgnoreCase)
                    ))
                {
                    //server is not in current list.
                    server dnsSrvr;
                    try
                    {
                        dnsSrvr = server.dnslookup(NetCon.ServerName);

                        //query database and add if necessary.
                        var dbSrvr = dataOper.getServer(dnsSrvr.hostname, dnsSrvr.domain);

                        if (dbSrvr == null)
                        {
                            //server was not found; add to Database.
                            dataOper.Insert<server>(dnsSrvr);
                            globalLog.Write("Added new Server to [servers]: " + dnsSrvr.hostname + dnsSrvr.domain);
                        }
                        servers.Add(dataOper.getServer(dnsSrvr.hostname, dnsSrvr.domain));
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

                share fileshare = dataOper.getShare(currentServer.serverID, NetCon.ShareName);

                if (fileshare == null)
                {
                    fileshare = new share(currentServer, NetCon.ShareName);
                    dataOper.Insert<share>(fileshare);
                    globalLog.Write("Added new Share to [shares]: " + fileshare.server.hostname + "\\" + fileshare.shareName);

                    //Re-Get the share
                    fileshare = dataOper.getShare(currentServer.serverID, NetCon.ShareName);
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
                        x => x.ServerName.Equals(fileshare.server.hostname, StringComparison.OrdinalIgnoreCase) &&
                             x.ShareName.Equals(fileshare.shareName, StringComparison.OrdinalIgnoreCase)
                        );
                    map = new mapping(fileshare, CurrentUser, CurrentComputer, NetCon.LocalName, NetCon.UserName);

                    dataOper.Insert<mapping>(map);
                    logger.Write("Added new Mapping to [mappings]: " + CurrentUser.username + "@" + CurrentComputer.hostname + ": " + map.letter + ": " + map.share.server.hostname + "\\" + map.share.shareName);
                    map = dataOper.getMapping(fileshare.shareID, CurrentUser.userID, CurrentComputer.computerID);
                }

                mappedList.Add(map);
            }

            //update date field on found servers and found mappings.
            string now = DateTime.Now.ToString();
            foreach (mapping map in mappedList)
            {
                map.date = now;
            }

            foreach (server srvr in servers)
            {
                srvr.date = now;
            }

            db.SaveChanges();
        }
    }
}
