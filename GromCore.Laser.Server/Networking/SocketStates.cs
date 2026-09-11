using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GromCore.Laser.Server.Networking
{
    public enum SocketState
    {
        NowOpened = 1,
        ClosingStage = 2,
        NowClosed = 3,
        OpeningStage = 4,
        WaitingForUpdate = 5, // хз где использовать буду но пусть будет
    }
}
