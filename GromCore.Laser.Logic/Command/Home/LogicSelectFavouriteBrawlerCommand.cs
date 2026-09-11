namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicSelectFavouriteBrawlerCommand : Command
    {
        public int CharacterId;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CharacterId=ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            var d = DataTables.Get(16).GetDataByGlobalId<CharacterData>(CharacterId);
            if (d != null && !d.Disabled && homeMode.Avatar.HasHero(CharacterId))
            {
                homeMode.Home.FavouriteCharacter = CharacterId;
                return 0;
            }


            return -1;
        }

        public override int GetCommandType()
        {
            return 570;
        }
    }
}
