using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fileshare_utility
{
    /// <summary>
    /// Used to perform Entity Framework Queries
    /// </summary>
    public class DatabaseService : IRepository, IDisposable
    {
        bool disposed = false;
        private DataContext db;
        private IWriter Logger;

        public DatabaseService() : this(new DataContext(), new FileWriter()) { }
        public DatabaseService(string filePath) : this(new DataContext(), new FileWriter(filePath)) { }
        public DatabaseService(DataContext db, IWriter logger)
            : base()
        {
            this.db = db;
            this.Logger = logger;
        }

        public T Get<T>(T Entity)
            where T : class, Entity<T>, new()
        {
            return db.Set<T>().FirstOrDefault<T>(Entity.BuildExpression());
        }

        public T FindOrInsert<T>(T Entity)
            where T : class, Entity<T>, new()
        {
            //Check if exists
            T Returned = Get<T>(Entity);

            if (Returned == null)
            {
                //Add and Get
                Insert<T>(Entity);
                Returned = Get<T>(Entity);
            }

            return Returned;
        }

        public void Insert<T>(T Entity)
            where T : class, new()
        {
            db.Set<T>().Add(Entity);
            Logger.Output("Added " + typeof(T).ToString() + " to [" + GetTableName<T>(Entity) + "]:" + Entity.ToString());
            db.SaveChanges();
        }

        public string GetTableName<T>(T Entity)
            where T : class
        {
            Regex regex = new Regex("(?<=FROM \\[)[A-z]*(?=\\] AS)");
            string match = regex.Match(db.Set<T>().ToString()).ToString();

            return match;
        }

        public void SaveChanges()
        {
            db.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                //Dispose of the DBContext
                db.Dispose();
            }

            disposed = true;
        }

        /// <summary>
        /// Builds the database, Note: This is a static build and this will not work if you change the entity model.
        /// EF6 and Code First do not play well with SQLite.
        /// </summary>
        public void InitOutput()
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

            using (SQLiteConnection dbConnection = new SQLiteConnection(db.Database.Connection.ConnectionString))
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
