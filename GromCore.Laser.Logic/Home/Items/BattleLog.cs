namespace GromCore.Laser.Logic.Home.Items
{
    using System.IO;
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;

    public class BattleLogs
    {
        [JsonProperty("log_list")]
        public List<BattleLogEntry> LogList;

        public BattleLogs()
        {
            LogList=new();
        }

        public void Add(BattleLogEntry log)
        {
            log.IternalId = GetIndex();
            LogList.Add(log);
        }

        public int GetIndex() => LogList.Count;

        public void Encode(ByteStream stream)
        {
            stream.WriteBoolean(true);
            stream.WriteVInt(Math.Min(LogList.Count, 23));

            foreach (var log in LogList.Skip(Math.Max(0, LogList.Count - 23)).Reverse())
            {
                log.Encode(stream);
            }
        }
    }
}
