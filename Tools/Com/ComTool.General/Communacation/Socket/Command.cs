using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComTool.General.Communacation
{
    public enum Command
    {
        RequestSendFile = 1,
        ResponeSendFile = 1048577,
        RequestSendFilePack = 2,
        ResponeSendFilePack = 1048578,
        RequestCancelSendFile = 3,
        ResponeCancelSendFile = 1048579,
        RequestCancelReceiveFile = 4,
        ResponeCancelReceiveFile = 1048580,
        RequestSendTextMSg = 16
    }

    public enum MsgType
    {
        TxtMsg,
        Shake,
        Face,
        Pic
    }

    public enum SocketError
    {

    }

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
