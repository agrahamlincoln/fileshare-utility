using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    class MappedShare : NetworkConnection
    {
        public mapping mapping;

        public MappedShare(NetworkConnection NetCon)
            : base(NetCon) 
        {
            this.mapping = new mapping(NetCon.LocalName, NetCon.UserName);
        }
    }
}
