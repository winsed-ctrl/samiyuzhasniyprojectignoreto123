namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Team;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSelectSkinCommand : Command
    {
        int skinId;
        public bool setByServer;

        public void SetSkinIdByServer(int skinid)
        {
            setByServer = true;
            skinId = skinid;
        }
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            if(!setByServer) skinId = stream.ReadVInt();
            stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            SkinData skinData = DataTables.Get(DataType.Skin).GetData<SkinData>(skinId);
            if (skinData == null) return -1;
            if (!skinData.Name.EndsWith("Default") && !homeMode.Home.UnlockedSkins.Contains(skinData.GetGlobalId())) return -1;
            SkinConfData skinConfData= DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(skinData.Conf);
            int characterid=DataTables.Get(16).GetData<CharacterData>(skinConfData.Character).GetInstanceId() + 16000000;
            Hero hero = homeMode.Avatar.GetHero(characterid);
            if (hero != null)  hero.SelectedSkinId = skinId;
            homeMode.CharacterChanged.Invoke(0);
            return 0;
        }


        public override int GetCommandType()
        {
            return 506;
        }
    }
}
