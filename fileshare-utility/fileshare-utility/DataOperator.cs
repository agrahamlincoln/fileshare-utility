using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    class DataOperator
    {
        DataContext db;

        public DataOperator()
        {
            this.db = new DataContext();
        }

        public DataOperator(DataContext db)
        {
            this.db = db;
        }

        #region accessors
        public computer getComputer(string hostname)
        {
            return db.computers.FirstOrDefault<computer>(
                    x => x.hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase)
                    );
        }

        public server getServer(string hostname, string domain)
        {
            return db.servers.FirstOrDefault<server>(
                    x => x.hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase) &&
                         x.domain.Equals(domain, StringComparison.OrdinalIgnoreCase)
                    );
        }

        public share getShare(long serverID, string shareName)
        {
            return db.shares.FirstOrDefault<share>(
                    x => x.serverID == serverID &&
                         x.shareName.Equals(shareName, StringComparison.OrdinalIgnoreCase)
                    );
        }

        public user getUser(string username)
        {
            return db.users.FirstOrDefault<user>(
                    x => x.username.Equals(username, StringComparison.OrdinalIgnoreCase)
                    );
        }

        public mapping getMapping(long shareID, long userID, long computerID)
        {
            return db.mappings.FirstOrDefault<mapping>(
                    x => x.shareID == shareID &&
                         x.userID == userID &&
                         x.computerID == computerID
                    );
        }
        #endregion

        #region add

        public void Insert<T>(T Entity)
            where T: class
        {
            db.Set<T>().Add(Entity);
            db.SaveChanges();
        }

        #endregion
    }
}
