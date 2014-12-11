using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    public interface Entity<T>
    {
         Expression<Func<T, bool>> BuildExpression();
    }

    partial class computer : Entity<computer>
    {
        /// <summary>
        /// Single-Arg Constructor
        /// </summary>
        /// <param name="hostname">Hostname of the computer</param>
        public computer(string hostname)
            : this()
        {
            this.hostname = hostname.ToUpper();
        }

        public Expression<Func<computer, bool>> BuildExpression()
        {
            var entityType = typeof(computer);
            var propertyName = "hostname";
            var thisValue = this.hostname;

            ParameterExpression entity = Expression.Parameter(entityType, "x");
            MemberExpression entityProperty = Expression.Property(entity, propertyName);
            ConstantExpression thisProperty = Expression.Constant(thisValue, typeof(string));
            Expression predicateBody = Expression.Equal(entityProperty, thisProperty);

            var lambda = Expression.Lambda<Func<computer, bool>>(
                predicateBody, 
                new ParameterExpression[] { entity }
                );

            return lambda;                
        }

        public override bool Equals(object obj)
        {
            var item = obj as computer;

            if (obj == null)
                return false;

            return this.hostname.Equals(item.hostname, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return this.hostname + " (" + computerID + ")";
        }
    }

    partial class mapping : Entity<mapping>
    {
        /// <summary>
        /// No-Arg constructor, initializes values to un-real
        /// </summary>
        public mapping()
        {
            this.shareID = -1;
            this.computerID = -1;
            this.userID = -1;
            this.letter = "C";
            this.username = "";
            this.date = DateTime.MinValue.ToString();
        }

        /// <summary>
        /// Full-Arg Constructor
        /// </summary>
        /// <param name="fileshare">Share that is mapped</param>
        /// <param name="usr">User with the mapping</param>
        /// <param name="comp">Computer with the mapping</param>
        /// <param name="letter">Letter the mapping is assigned to</param>
        /// <param name="username">Username used for mapping</param>
        public mapping(share fileshare, user usr, computer comp, string letter, string username)
        {
            this.shareID = fileshare.shareID;
            this.userID = usr.userID;
            this.computerID = comp.computerID;
            this.letter = letter;
            this.username = username;
            this.date = DateTime.Now.ToString();
        }

        public Expression<Func<mapping, bool>> BuildExpression()
        {
            var entityType = typeof(mapping);
            var property1Name = "shareID";
            var property2Name = "userID";
            var property3Name = "computerID";
            var thisValue1 = this.shareID;
            var thisValue2 = this.userID;
            var thisValue3 = this.computerID;

            ParameterExpression entity = Expression.Parameter(entityType, "x");

            MemberExpression entityProperty1 = Expression.Property(entity, property1Name);
            ConstantExpression thisProperty1 = Expression.Constant(thisValue1, typeof(long));

            MemberExpression entityProperty2 = Expression.Property(entity, property2Name);
            ConstantExpression thisProperty2 = Expression.Constant(thisValue2, typeof(long));

            MemberExpression entityProperty3 = Expression.Property(entity, property3Name);
            ConstantExpression thisProperty3 = Expression.Constant(thisValue3, typeof(long));

            BinaryExpression expr1 = Expression.Equal(entityProperty1, thisProperty1);
            BinaryExpression expr2 = Expression.Equal(entityProperty2, thisProperty2);
            BinaryExpression expr3 = Expression.Equal(entityProperty3, thisProperty3);

            Expression predicateBody = Expression.AndAlso(Expression.AndAlso(expr1, expr2), expr3);

            var lambda = Expression.Lambda<Func<mapping, bool>>(
                predicateBody,
                new ParameterExpression[] { entity }
                );

            return lambda;
        }

        public override bool Equals(object obj)
        {
            var item = obj as mapping;

            if (item == null)
                return false;

            return (this.shareID.Equals(item.shareID) &&
                    this.userID.Equals(item.userID) &&
                    this.computerID.Equals(item.computerID)
                   );
        }

        public override string ToString()
        {
            return this.user.ToString() + "@" + this.computer.ToString() + ": " + this.letter + " " + this.share.ToString();
        }
    }

    partial class master : Entity<master>
    {
        public master() : this(null, null) { }
        public master(string setting) : this(setting, null) { }
        public master(string setting, string value)
        {
            this.ID = -1;
            this.setting = setting;
            this.value = value;
        }

        public void increment()
        {
            try
            {
                int intvalue = Convert.ToInt32(this.value);

                intvalue++;

                this.value = intvalue.ToString();
            }
            catch (FormatException crap)
            {
                //Value is not a number
            }
            catch (OverflowException crap)
            {
                //Value is either less than Int32.min value or greater than Int32.max
            }
        }

        public Expression<Func<master, bool>> BuildExpression()
        {
            var entityType = typeof(master);
            var propertyName = "setting";
            var thisValue = this.setting;

            ParameterExpression entity = Expression.Parameter(entityType, "x");

            MemberExpression entityProperty = Expression.Property(entity, propertyName);
            ConstantExpression thisProperty = Expression.Constant(thisValue, typeof(string));

            BinaryExpression predicateBody = Expression.Equal(entityProperty, thisProperty);

            var lambda = Expression.Lambda<Func<master, bool>>(
                predicateBody,
                new ParameterExpression[] { entity }
                );

            return lambda;
        }

        public override bool Equals(object obj)
        {
            var item = obj as master;

            if (item == null)
                return false;

            var setting1 = this.setting ?? String.Empty;
            var setting2 = item.setting ?? String.Empty;
            var value1 = this.value ?? String.Empty;
            var value2 = item.value ?? String.Empty;

            return (setting1.Equals(setting2, StringComparison.OrdinalIgnoreCase) &&
                    value1.Equals(value2, StringComparison.OrdinalIgnoreCase)
                   );
        }

        public override string ToString()
        {
            if (this.setting == null)
                return "";
            if (this.setting != null && this.value == null)
                return this.setting;

            return this.setting + ": " + this.value;
        }
    }

    partial class server : Entity<server>
    {
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="srvr">server object to copy</param>
        public server(server srvr)
        {
            this.serverID = srvr.serverID;
            this.hostname = srvr.hostname.ToUpper();
            this.domain = srvr.domain;
            this.active = srvr.active;
            this.date = srvr.date;
            this.shares = srvr.shares;
        }

        /// <summary>
        /// Two-Arg Constructor
        /// </summary>
        /// <param name="hostname">Hostname of server</param>
        /// <param name="domain">Domain the server belongs to</param>
        public server(string hostname, string domain)
            : this()
        {
            this.hostname = hostname.ToUpper();
            this.domain = domain;
            this.active = true;
            this.date = DateTime.Now.ToString();
        }

        /// <summary>
        /// Performs a DNS lookup on an address and returns a server object
        /// </summary>
        /// <param name="address">IP Address/Hostname of a server</param>
        /// <returns>null if not resolved; server object representing the resolved host.</returns>
        /// <exception cref="ArgumentNullException">Argument is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Argument is more than 255 chars</exception>
        /// <exception cref="SocketException">Host was not resolvable</exception>
        /// <exception cref="ArgumentException">Invalid IP Address</exception>
        public static server dnslookup(string address)
        {
            server resolved = null;

            IPHostEntry hostEntry;
            hostEntry = Dns.GetHostEntry(address.ToUpper());

            var host = hostEntry.HostName.Split('.').ToList<string>();

            var name = host.ElementAt(0);
            host.RemoveAt(0);
            var domain = String.Join(".", host.ToArray());

            resolved = new server(name, domain);

            return resolved;
        }

        public Expression<Func<server, bool>> BuildExpression()
        {
            var entityType = typeof(server);
            var property1Name = "hostname";
            var property2Name = "domain";
            var thisValue1 = this.hostname;
            var thisValue2 = this.domain;

            ParameterExpression entity = Expression.Parameter(entityType, "x");

            MemberExpression entityProperty1 = Expression.Property(entity, property1Name);
            ConstantExpression thisProperty1 = Expression.Constant(thisValue1, typeof(string));

            MemberExpression entityProperty2 = Expression.Property(entity, property2Name);
            ConstantExpression thisProperty2 = Expression.Constant(thisValue2, typeof(string));

            BinaryExpression expr1 = Expression.Equal(entityProperty1, thisProperty1);
            BinaryExpression expr2 = Expression.Equal(entityProperty2, thisProperty2);

            Expression predicateBody = Expression.AndAlso(expr1, expr2);

            var lambda = Expression.Lambda<Func<server, bool>>(
                predicateBody,
                new ParameterExpression[] { entity }
                );

            return lambda;
        }

        public override bool Equals(object obj)
        {
            var item = obj as server;

            if (item == null)
                return false;

            return (this.hostname.Equals(item.hostname, StringComparison.OrdinalIgnoreCase) && 
                    this.domain.Equals(item.domain, StringComparison.OrdinalIgnoreCase)
                   );
        }

        public override string ToString()
        {
            return this.hostname + "." + this.domain + " (" + serverID + ")";
        }
    }

    partial class share : Entity<share>
    {
        /// <summary>
        /// Two-Arg constructor
        /// </summary>
        /// <param name="currServer">Server Object that the share belongs to</param>
        /// <param name="shareName">Shared name of the share</param>
        public share(server currServer, string shareName)
            : this()
        {
            this.serverID = currServer.serverID;
            this.shareName = shareName;
            this.server = currServer;
            this.active = true;
        }

        public Expression<Func<share, bool>> BuildExpression()
        {
            var entityType = typeof(share);
            var property1Name = "serverID";
            var property2Name = "shareName";
            var thisValue1 = this.serverID;
            var thisValue2 = this.shareName;

            ParameterExpression entity = Expression.Parameter(entityType, "x");

            MemberExpression entityProperty1 = Expression.Property(entity, property1Name);
            ConstantExpression thisProperty1 = Expression.Constant(thisValue1, typeof(long));

            MemberExpression entityProperty2 = Expression.Property(entity, property2Name);
            ConstantExpression thisProperty2 = Expression.Constant(thisValue2, typeof(string));

            BinaryExpression expr1 = Expression.Equal(entityProperty1, thisProperty1);
            BinaryExpression expr2 = Expression.Equal(entityProperty2, thisProperty2);

            Expression predicateBody = Expression.AndAlso(expr1, expr2);

            var lambda = Expression.Lambda<Func<share, bool>>(
                predicateBody,
                new ParameterExpression[] { entity }
                );

            return lambda;
        }

        public override bool Equals(object obj)
        {
            var item = obj as share;

            if (item == null)
                return false;

            return (this.serverID.Equals(item.serverID) && 
                    this.shareName.Equals(item.shareName, StringComparison.OrdinalIgnoreCase)
                   );
        }

        public override string ToString()
        {
            return this.server.hostname + "\\" + this.shareName + " (" + this.shareID + ")";
        }
    }

    partial class user : Entity<user>
    {
        /// <summary>
        /// Single-Arg Constructor
        /// </summary>
        /// <param name="username">Username of the user</param>
        public user(string username)
            : this()
        {
            this.username = username;
        }

        public Expression<Func<user, bool>> BuildExpression()
        {
            var entityType = typeof(user);
            var propertyName = "username";
            var thisValue = this.username;

            ParameterExpression entity = Expression.Parameter(entityType, "x");
            MemberExpression entityProperty = Expression.Property(entity, propertyName);
            ConstantExpression thisProperty = Expression.Constant(thisValue, typeof(string));
            Expression predicateBody = Expression.Equal(entityProperty, thisProperty);

            var lambda = Expression.Lambda<Func<user, bool>>(
                predicateBody,
                new ParameterExpression[] { entity }
                );

            return lambda;
        }

        public override bool Equals(object obj)
        {
            var item = obj as user;

            if (item == null)
                return false;

            return this.username.Equals(item.username, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return this.username + " (" + this.userID + ")";
        }
    }
}
