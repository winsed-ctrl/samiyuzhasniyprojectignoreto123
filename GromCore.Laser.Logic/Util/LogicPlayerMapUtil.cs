using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GromCore.Laser.Logic.Home;

namespace GromCore.Laser.Logic.Util
{
    public static class LogicPlayerMapUtil
    {
        public static bool isPlayerEligibleToCreateMap(bool a1, ClientHome a2)
        {
            bool result; // x0
            int v4; // w19

            result = false;
            if (a1)
            {
                if (a2 != null)
                {
                    result = false;// a2.logicConfData.Contains(10027); // 27LL
                }
            }
            return result;
        }
    }
}
//You dont belong here, you know it?