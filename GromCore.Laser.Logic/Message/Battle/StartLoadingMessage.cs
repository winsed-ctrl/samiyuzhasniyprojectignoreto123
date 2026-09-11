namespace GromCore.Laser.Logic.Message.Battle
{
    using System.Buffers;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.Util;

    public class StartLoadingMessage : GameMessage
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
        public StartLoadingMessage() : base()
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
            bool isHighTrophyMode = Players.Count(p =>p.Home != null &&p.Avatar != null && p.Avatar.GetHero(p.Home.CharacterIds[0])?.Trophies > 950) >= 2;

            Stream.WriteInt(Players.Count);
            Stream.WriteInt(OwnIndex);
            Stream.WriteInt(TeamIndex);

            Stream.WriteInt(Players.Count);
            foreach (BattlePlayer player in Players)
            {
                bool isOwnTeam = player.TeamIndex == TeamIndex || player.PlayerIndex == OwnIndex;
                bool shouldHideName = isHighTrophyMode && !isOwnTeam && !LeaveButton;
                player.Encode(Stream, shouldEncodeName: !shouldHideName);
            }

            Stream.WriteInt(0); // array

            List<int> t = Modifiers.ToList();
            Stream.WriteInt(t.Count);
            foreach (int modifier in t) Stream.WriteInt(modifier);

            Stream.WriteInt(0); // randomseed

            Stream.WriteVInt(GameType);//GameMode
            Stream.WriteVInt(MapMode);
            Stream.WriteVInt(GameMode);//gamemode varidatino
            Stream.WriteVInt(0);
            Stream.WriteBoolean(true);
            Stream.WriteVInt(SpectateMode);
            Stream.WriteVInt(1); // difficulty

            ByteStreamHelper.WriteDataReference(Stream,LocationId);
            ByteStreamHelper.WriteBattlePlayerMap(Stream,map);

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(Round); // round
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

            Stream.WriteBoolean(LeaveButton);//show quit

        }

        public override int GetMessageType()
        {
            return 20559;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
