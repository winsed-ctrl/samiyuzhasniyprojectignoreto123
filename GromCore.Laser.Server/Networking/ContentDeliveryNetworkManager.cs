using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Text.Json;

namespace GromCore.Laser.Server.Networking
{

    public class ContentDeliveryNetworkManager
    {
        public class BlockedContentModel
        {
            public List<int> hyper { get; set; }
            public List<int> acses { get; set; }
            public List<int> pass { get; set; }
            public List<int> character { get; set; }
        }
        private static HttpClient client;
        public static void Init()
        {
            client = new();
            Console.WriteLine("[CDNManager] Ready to work.");
        }
        public static List<int> GetIntListFromField(string field)
        {
            var response = client.GetAsync("").Result;

            if (!response.IsSuccessStatusCode)
                return null;
            BlockedContentModel data = JsonSerializer.Deserialize<BlockedContentModel>(response.Content.ReadAsStringAsync().Result);

            if (data == null) return null;

            return field switch
            {
                "Overcharge" => data.hyper,
                "Gadget" => data.acses,
                "SPG" => data.pass,
                "Brawler" => data.character,
                _ => null
            };
        }
    }

}
