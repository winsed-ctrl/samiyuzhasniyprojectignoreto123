global using GromCore.Laser.Logic.Data.Helper;
global using GromCore.Laser.Logic.Data.Reader;

namespace GromCore.Laser.Logic.Data
{
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Data.Reader;
    using GromCore.Laser.Titan.Debug;

    public partial class DataTables
    {
        public static readonly Dictionary<DataType, string> Gamefiles = new Dictionary<DataType, string>();
        private static Gamefiles Tables;

        private static void AddFilesToLoad()
        {
            string csv_folder = "csv_logic";
            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark") csv_folder = "csv_logic_custom";
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom") csv_folder = "csv_logic_grom";
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "retro") csv_folder = "csv_logic_retro";


            Gamefiles.Add(DataType.Projectile, $"Assets/{csv_folder}/projectiles.csv");
            Gamefiles.Add(DataType.AllianceBadge, $"Assets/{csv_folder}/alliance_badges.csv");
            Gamefiles.Add(DataType.Location, $"Assets/{csv_folder}/locations.csv");
            Gamefiles.Add(DataType.Character, $"Assets/{csv_folder}/characters.csv");
            Gamefiles.Add(DataType.AreaEffect, $"Assets/{csv_folder}/area_effects.csv");
            Gamefiles.Add(DataType.Item, $"Assets/{csv_folder}/items.csv");
            Gamefiles.Add(DataType.Map, $"Assets/{csv_folder}/maps.csv");
            Gamefiles.Add(DataType.Skill, $"Assets/{csv_folder}/skills.csv");
            Gamefiles.Add(DataType.Card, $"Assets/{csv_folder}/cards.csv");
            Gamefiles.Add(DataType.Tile, $"Assets/{csv_folder}/tiles.csv");
            Gamefiles.Add(DataType.PlayerThumbnail, $"Assets/{csv_folder}/player_thumbnails.csv");
            Gamefiles.Add(DataType.Skin, $"Assets/{csv_folder}/skins.csv");
            Gamefiles.Add(DataType.Milestone, $"Assets/{csv_folder}/milestones.csv");
            Gamefiles.Add(DataType.SkinConf, $"Assets/{csv_folder}/skin_confs.csv");
            Gamefiles.Add(DataType.Accessory, $"Assets/{csv_folder}/accessories.csv");
            Gamefiles.Add(DataType.Emote, $"Assets/{csv_folder}/emotes.csv");
            Gamefiles.Add(DataType.Gear, $"Assets/{csv_folder}/gear_boosts.csv");
            Gamefiles.Add(DataType.NameColor, $"Assets/{csv_folder}/name_colors.csv");
            Gamefiles.Add(DataType.Titul, $"Assets/{csv_folder}/player_titles.csv");
            Gamefiles.Add(DataType.Region, $"Assets/{csv_folder}/regions.csv");
            Gamefiles.Add(DataType.MasteryVanity, $"Assets/{csv_folder}/mastery_hero_confs.csv");
            Gamefiles.Add(DataType.Mastery, $"Assets/{csv_folder}/mastery_levels.csv");
            Gamefiles.Add(DataType.Spray, $"Assets/{csv_folder}/sprays.csv");
            Gamefiles.Add(DataType.RankedE, $"Assets/{csv_folder}/ranked_locations.csv");
        }

        public static void Load()
        {
            DataTables.AddFilesToLoad();

            Tables = new Gamefiles();

            foreach (var file in Gamefiles)
            {
                Tables.Initialize(new Table(file.Value), file.Key);
                //Debugger.Print(file.Value);
            }

            Debugger.Print($"{Gamefiles.Count} Data Tables initialized!");
        }

        public static bool TableExists(int t)
        {
            return Tables.ContainsTable(t);
        }

        public static DataTable Get(DataType classId)
        {
            return Tables.Get(classId);
        }

        public static DataTable Get(int classId)
        {
            return Tables.Get(classId);
        }
        public static CharacterData GetCharacterByName(string name)
        {
            return Get(16).GetData<CharacterData>(name);
        }
        public static ProjectileData GetProjectileByName(string name)
        {
            return Get(6).GetData<ProjectileData>(name);
        }
        public static AreaEffectData GetAreaEffectByName(string name)
        {
            return Get(DataType.AreaEffect).GetData<AreaEffectData>(name);
        }
        public static ItemData GetItemByName(string name)
        {
            return Get(DataType.Item).GetData<ItemData>(name);
        }
        public static CardData GetUnlockCardFor(CharacterData character)
        {
            foreach(CardData i in Get(DataType.Card).GetDatas())
            {
                if (i.Target == character.Name && i.Type == "unlock") return i;
            }
            return null;
        }
    }
}