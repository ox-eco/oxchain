using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OX.Bapps
{
    public delegate void BappEventHandler();
    public delegate void BappEventHandler<TEventArgs>(TEventArgs e);
}
