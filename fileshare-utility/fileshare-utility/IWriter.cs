using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileshare_utility
{
    public interface IWriter
    {
        string Header();
        
        void Output(string message);
    }
}
