using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GromCore.Laser.Titan.Cryptography;

namespace GromCore.Laser.Titan.Util
{
    public static class Start
    {
        private static string serverSocket;
        public static bool StartSocket(string ip, int port)
        {
            string Ip = ip;
            int Port = 9339;
            //if (Ip != ip || Port != port) throw new ArgumentNullException(nameof(serverSocket), "ServerSocket is NULL! Please reinstall your vds/OS");
            
            return true;
        }
    }
}
