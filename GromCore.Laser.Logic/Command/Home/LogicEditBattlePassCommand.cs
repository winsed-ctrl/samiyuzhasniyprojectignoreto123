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
    using System.Net.Http.Headers;

    public class LogicEditBattlePassCommand : Command
    {
        public int CharacterId;
        public int VanityId;
        public bool unk1;
        public int Index;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CharacterId = ByteStreamHelper.ReadDataReference(stream);
            VanityId = ByteStreamHelper.ReadDataReference(stream);
            unk1 = stream.ReadBoolean();
            Index = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            switch (Index)
            {
                case 0:
                    PlayerThumbnailData icon = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(VanityId);
                    if(VanityId == 0)
                    {
                        homeMode.Home.DefaultBattleCard.Thumbnail1 = 0;
                        return 0;
                    }
                    if (icon == null) return -1;
                    if (icon.Name != "base1" && icon.RequiredTotalTrophies == 0 && icon.RequiredHero == null && !homeMode.Home.UnlockedThumbnails.Contains(VanityId)) return -1;
                    if (!String.IsNullOrEmpty(icon.RequiredHero) && !homeMode
                        .Avatar
                        .HasHero(
                        DataTables.Get(16)
                        .GetData<CharacterData>(
                            icon.RequiredHero)
                        .GetGlobalId())) return -1;
                    homeMode.Home.DefaultBattleCard.Thumbnail1 = VanityId;
                    break;
                case 1:
                    if (VanityId == 0)
                    {
                        homeMode.Home.DefaultBattleCard.Thumbnail2 = 0;
                        return 0;
                    }
                    PlayerThumbnailData icon1 = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(VanityId);
                    if (icon1 == null) return -1;
                    if (icon1.Name != "base1"  && icon1.RequiredTotalTrophies == 0 &&  icon1.RequiredHero == null && !homeMode.Home.UnlockedThumbnails.Contains(VanityId)) return -1;
                    if (!String.IsNullOrEmpty(icon1.RequiredHero) && !homeMode
                        .Avatar
                        .HasHero(
                        DataTables.Get(16)
                        .GetData<CharacterData>(
                            icon1.RequiredHero)
                        .GetGlobalId())) return -1;
                    homeMode.Home.DefaultBattleCard.Thumbnail2 = VanityId;
                    break;
                case 5:
                    if (VanityId == 0)
                    {
                        homeMode.Home.DefaultBattleCard.Emote = 0;
                        return 0;
                    }
                    EmoteData emoteData = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(VanityId);
                    if (emoteData == null && emoteData.Rarity != "DEFAULT" && !homeMode.Home.UnlockedEmotes.Contains(VanityId)) return -1;
                    if (!String.IsNullOrEmpty(emoteData.Character) && !homeMode
                        .Avatar
                        .HasHero(
                        DataTables.Get(16)
                        .GetData<CharacterData>(
                            emoteData.Character)
                        .GetGlobalId())) return -1;
                    homeMode.Home.DefaultBattleCard.Emote = VanityId;
                    break;
                case 10:
                    if (VanityId == 0)
                    {
                        homeMode.Home.DefaultBattleCard.Title = 0;
                        return 0;
                    }
                    if (!homeMode.Home.UnlockedTituls.Contains(VanityId)) return -1;
                    homeMode.Home.DefaultBattleCard.Title = VanityId;
                    break;
                default:
                    return -1;
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 568;
        }
    }
}
