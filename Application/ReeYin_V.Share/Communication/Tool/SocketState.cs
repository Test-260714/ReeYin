using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Share.Communication.Tool
{
    public enum SocketState
    {
        Connecting,
        Connected,
        Reconnection,
        Disconnect,
        StartListening,
        StopListening,
        ClientOnline,
        ClientOnOff
    }
}
