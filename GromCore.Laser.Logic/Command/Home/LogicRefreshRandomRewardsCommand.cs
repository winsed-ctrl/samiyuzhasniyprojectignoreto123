namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Titan.DataStream;
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public class LogicRefreshRandomRewardsCommand : Command
    {
        public int VisualRarity;
        public int LogicRarity = -1;
        public bool StopRender;
        public HomeMode HomeModeCopy;

        public override int Execute(HomeMode homeMode)
        {
            HomeModeCopy = homeMode;
            if (homeMode.Home.ExecuteLobbyDrop) homeMode.Home.ExecuteLobbyDrop = false;
            return 0;
        }
        public override void Encode(ByteStream stream)
        {
            if (LogicRarity == -1) HomeModeCopy.Home.StarrDrop.GenerateDrop(HomeModeCopy);
            stream.WriteVInt(1);
            stream.WriteVInt(-1);
            stream.WriteVInt(-1);
            stream.WriteVInt(0);
            stream.WriteVInt(1);
            stream.WriteVInt(1);

            stream.WriteVInt(5);
            for (int i = 0; i < 5; i++)
            {
                stream.WriteDataReference(80, i);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
            }

            if (!StopRender)
            {
                stream.WriteBoolean(true);
                {
                    stream.WriteDataReference(80, (int)HomeModeCopy.Home.StarrDrop.Rarity);
                    stream.WriteVInt(1);
                    {
                        stream.WriteVInt(HomeModeCopy.Home.StarrDrop.Data.Type);
                        stream.WriteVInt(HomeModeCopy.Home.StarrDrop.Data.Ammount);
                        stream.WriteDataReference(HomeModeCopy.Home.StarrDrop.Data.SkinGlobalID);
                        stream.WriteVInt(HomeModeCopy.Home.StarrDrop.Data.DataGlobalID);

                        stream.WriteVInt(0);
                        stream.WriteVInt(0);
                    }
                }
            }
            else
            {
                stream.WriteBoolean(false);
                stream.WriteBoolean(false);
            }
            stream.WriteInt(-1);
            stream.WriteVInt(LogicRarity == -1 ? HomeModeCopy.Avatar.Wins + 1 : HomeModeCopy.Avatar.Wins); // Battle Progression Step
            stream.WriteVInt(0);
            stream.WriteVInt(0); // timer until refresh
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            base.Encode(stream);
            
        }



        public override int GetCommandType()
        {
            return 228;
        }
    }
}
