using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    /// <summary>
    /// Used to perform Entity Framework Queries
    /// </summary>
    partial class DataContext
    {
        private LogWriter dbLogger = new LogWriter("DBLog");

        public DataContext(string logPath)
            : this()
        {
            dbLogger.logPath = logPath;
        }

        #region accessors
        /// <summary>
        /// Retrieves the first computer that matches the hostname
        /// </summary>
        /// <param name="hostname">Hostname of computer</param>
        /// <returns>Null if none found</returns>
        public computer getComputer(string hostname)
        {
            return computers.FirstOrDefault<computer>(
                    x => x.hostname.ToUpper() == hostname.ToUpper()
                    );
        }

        /// <summary>
        /// Retrieves the first server that matches the hostname and domain
        /// </summary>
        /// <param name="hostname">Hostname of server</param>
        /// <param name="domain">Domain of server</param>
        /// <returns>Null if none found</returns>
        public server getServer(string hostname, string domain)
        {
            return servers.FirstOrDefault<server>(
                    x => x.hostname.ToUpper() == hostname.ToUpper() &&
                         x.domain.ToUpper() == domain.ToUpper()
                    );
        }

        /// <summary>
        /// Retrieves the first share that matches the serverID and shareName
        /// </summary>
        /// <param name="serverID">Database ID of Server</param>
        /// <param name="shareName">Shared name of fileshare</param>
        /// <returns>Null if none found</returns>
        public share getShare(long serverID, string shareName)
        {
            return shares.FirstOrDefault<share>(
                    x => x.serverID == serverID &&
                         x.shareName.ToUpper() == shareName.ToUpper()
                    );
        }

        /// <summary>
        /// Retrieves the first user that matches the username
        /// </summary>
        /// <param name="username">Username of user</param>
        /// <returns>Null if none found</returns>
        public user getUser(string username)
        {
            return users.FirstOrDefault<user>(
                    x => x.username.ToUpper() == username.ToUpper()
                    );
        }

        /// <summary>
        /// Retrieves the first mapping that matches the share, user, and computer ID's
        /// </summary>
        /// <param name="shareID">Database ID of share</param>
        /// <param name="userID">Database ID of user</param>
        /// <param name="computerID">Database ID of computer</param>
        /// <returns>Null if none found</returns>
        public mapping getMapping(long shareID, long userID, long computerID)
        {
            return mappings.FirstOrDefault<mapping>(
                    x => x.shareID == shareID &&
                         x.userID == userID &&
                         x.computerID == computerID
                    );
        }

        public master getMaster(string setting)
        {
            return masters.FirstOrDefault<master>(
                    x => x.setting.ToUpper() == setting.ToUpper()
                    );
        }
        #endregion

        #region add
        /// <summary>
        /// Generic method to add new entities to database
        ///// </summary>
        /// <typeparam name="T">Entity Class (computer; mapping; master; server; share; user)</typeparam>
        /// <param name="Entity">Entity Object</param>
        public void Insert<T>(T Entity)
            where T : class
        {
            Set<T>().Add(Entity);
            dbLogger.Write("Added " + typeof(T).ToString() + " to [" + Set<T>().ToString() + "] :" + Entity.ToString());
            SaveChanges();
        }
        #endregion

        /// <summary>
        /// Builds the database, Note: This is a static build and this will not work if you change the entity model.
        /// EF6 and Code First do not play well with SQLite.
        /// </summary>
        public void BuildDB()
        {
            string create_TblMaster = @"CREATE TABLE [master](
                [ID] integer NOT NULL,
                [setting] text,
                [value] text,
                PRIMARY KEY (ID))";
            string create_TblUsers = @"CREATE TABLE [users](
                [userID] integer NOT NULL,
                [username] text,
                PRIMARY KEY(userID))";
            string create_TblComputers = @"CREATE TABLE [computers](
                [computerID] integer NOT NULL,
                [hostname] VARCHAR(15),
                PRIMARY KEY(computerID))";
            string create_TblServers = @"CREATE TABLE [servers](
                [serverID] integer NOT NULL,
                [hostname] VARCHAR(15),
                [active] boolean NOT NULL,
                [domain] VARCHAR(255),
                [date] VARCHAR(255),
                PRIMARY KEY(serverID))";
            string create_TblShares = @"CREATE TABLE [shares](
                [shareID] integer NOT NULL, 
                [serverID] integer NOT NULL,
                [shareName] VARCHAR(255),
                [active] boolean NOT NULL,
                FOREIGN KEY(serverID) REFERENCES [servers](serverID),
                PRIMARY KEY(shareID))";
            string create_TblMappings = @"CREATE TABLE [mappings](
                [shareID] integer NOT NULL,
                [computerID] integer NOT NULL,
                [userID] integer NOT NULL,
                [letter] VARCHAR(2),
                [username] text,
                [date] VARCHAR(255),
                FOREIGN KEY (shareID) REFERENCES [shares](shareID),
                FOREIGN KEY (computerID) REFERENCES [computers](computerID),
                FOREIGN KEY (userID) REFERENCES [users](userID),
                PRIMARY KEY (shareID, computerID, userID))";

            using (SQLiteConnection dbConnection = new SQLiteConnection(Database.Connection.ConnectionString))
            {
                dbConnection.Open();

                //create sqlcommands
                SQLiteCommand create_master = new SQLiteCommand(create_TblMaster, dbConnection);
                SQLiteCommand create_users = new SQLiteCommand(create_TblUsers, dbConnection);
                SQLiteCommand create_computers = new SQLiteCommand(create_TblComputers, dbConnection);
                SQLiteCommand create_servers = new SQLiteCommand(create_TblServers, dbConnection);
                SQLiteCommand create_shares = new SQLiteCommand(create_TblShares, dbConnection);
                SQLiteCommand create_mappings = new SQLiteCommand(create_TblMappings, dbConnection);

                //execute all queries
                create_master.ExecuteNonQuery();
                create_users.ExecuteNonQuery();
                create_computers.ExecuteNonQuery();
                create_servers.ExecuteNonQuery();
                create_shares.ExecuteNonQuery();
                create_mappings.ExecuteNonQuery();

                dbConnection.Close();
            }
        }
    }
}
