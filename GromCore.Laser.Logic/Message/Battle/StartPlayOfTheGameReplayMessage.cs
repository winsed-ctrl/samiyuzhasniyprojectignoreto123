namespace GromCore.Laser.Logic.Message.Battle
{
    using System.Buffers;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Util;

    public class StartPlayOfTheGameReplayMessage : GameMessage
    {
        public List<BattlePlayer> Players;
        public int OwnIndex;
        public int TeamIndex;

        public int LocationId;
        public int GameMode;
        public int GameType;
        public int SpectateMode;
        public BattlePlayerMap map;
        public int MapMode;
        public List<int> Modifiers;
        public bool LeaveButton;
        public int Difficulty;
        public int Round;
        public StartPlayOfTheGameReplayMessage() : base()
        {
            Players = new List<BattlePlayer>();
            MapMode = 1;
            Modifiers = new();
        }
        public void SetPlayerMap(BattlePlayerMap map)
        {
            if (map != null)
            {
                MapMode = 5;
                this.map = map;
                LocationId = 0;
                GameMode = map.GMV;
                LeaveButton = false;
            }
        }
        public override void Encode()
        {
            BitStream BitStream = new(64);
            Stream.WriteInt(1);
            Stream.WriteInt(OwnIndex);
            Stream.WriteInt(TeamIndex);
            Stream.WriteInt(1);
            foreach (BattlePlayer player in Players)
            {
                player.Encode(Stream, true);
            }

            Stream.WriteVInt(GameMode);
            Stream.WriteInt(1);
            foreach (BattlePlayer player in Players)
            {
                Stream.WriteBytes(BitStream.GetByteArray(), BitStream.GetLength());
            }
        }

        public override int GetMessageType()
        {
            return 20560;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
