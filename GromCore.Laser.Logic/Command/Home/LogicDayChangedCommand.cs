namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicDayChangedCommand : Command
    {
        public EventData[] Events;
        public override void Encode(ByteStream stream)
        {

            stream.WriteBoolean(true);

            stream.WriteVInt(0);

            stream.WriteVInt(35); // event slot id
            stream.WriteVInt(1);
            stream.WriteVInt(2);
            stream.WriteVInt(3);
            stream.WriteVInt(4);
            stream.WriteVInt(5);
            stream.WriteVInt(6);
            stream.WriteVInt(7);
            stream.WriteVInt(8);
            stream.WriteVInt(9);
            stream.WriteVInt(10);
            stream.WriteVInt(11);
            stream.WriteVInt(12);
            stream.WriteVInt(13);
            stream.WriteVInt(14);
            stream.WriteVInt(15);
            stream.WriteVInt(16);
            stream.WriteVInt(17);
            stream.WriteVInt(18);
            stream.WriteVInt(19);
            stream.WriteVInt(20);
            stream.WriteVInt(21);
            stream.WriteVInt(22);
            stream.WriteVInt(23);
            stream.WriteVInt(24);
            stream.WriteVInt(25);
            stream.WriteVInt(26);
            stream.WriteVInt(27);
            stream.WriteVInt(28);
            stream.WriteVInt(29);
            stream.WriteVInt(30);
            stream.WriteVInt(31);
            stream.WriteVInt(32);
            stream.WriteVInt(33);
            stream.WriteVInt(34);
            stream.WriteVInt(35);

            stream.WriteVInt(Events.Length);
            foreach (EventData e in Events)
            {
                e.Encode(stream);

            }

            stream.WriteVInt(Events.Length);
            foreach (EventData e in Events)
            {
                e.Encode(stream, true);
            }


            ByteStreamHelper.WriteIntList(stream, new List<int> { 20, 35, 75, 140, 290, 480, 800, 1250, 1875, 2800 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 10, 25, 50, 99 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 150, 400, 1200, 2600 });




            stream.WriteVInt(ReleaseEntry.LogicReleaaseEntry.Length / 2);  // IntValueEntry
            for (int i = 0; i < ReleaseEntry.LogicReleaaseEntry.Length / 2; i++)
            {

                stream.WriteDataReference(ReleaseEntry.LogicReleaaseEntry[i * 2] / 1000000, ReleaseEntry.LogicReleaaseEntry[i * 2]);
                stream.WriteInt(ReleaseEntry.LogicReleaaseEntry[i * 2 + 1]);
                stream.WriteInt(0);
                stream.WriteInt(0);
                stream.WriteBoolean(false);
            }
            int[] LogicConfData = new int[]
            {
                1,41000000,
                10027,0,
                10029,20,
                10018,1,

                23000136,1,


                29, 27,
                48, 99999,
                79, 99999,
                80, 99999,
                65,2,
                66,0,
                47,41381,
                50,1,
                1100, 500,
                1101, 500,
                1003, 1,
                36,0,
                74,1,
                78,1,
                17,4,
                100046,1,
                87,1,
                63,1,
                1, 114,
                1, 113
            };
            LogicConfData[1] = 41000000 + 0;
            stream.WriteVInt(LogicConfData.Length / 2);  // IntValueEntry
            for (int i = 0; i < LogicConfData.Length / 2; i++)
            {
                stream.WriteVInt(LogicConfData[i * 2 + 1]);
                stream.WriteVInt(LogicConfData[i * 2]);
            }

            stream.WriteVInt(3); // Timed Int Value Entry

            stream.WriteVInt(14);
            stream.WriteVInt(1);
            stream.WriteVInt(0);
            stream.WriteVInt(739760);

            stream.WriteVInt(29); // skin theme timer
            stream.WriteVInt(10);
            stream.WriteVInt(79);
            stream.WriteVInt((int)(DateTime.Parse("2026-04-05 12:30:00") - DateTime.Now).TotalSeconds);

            stream.WriteVInt(29); // ץח קוע געמנמו
            stream.WriteVInt(10);
            stream.WriteVInt(0);
            stream.WriteVInt(1330340);

            stream.WriteVInt(0); ; //Custom Event

            stream.WriteVInt(0);

            stream.WriteVInt(0);

            stream.WriteVInt(2);
            stream.WriteVInt(1);
            stream.WriteVInt(2);
            stream.WriteVInt(2);
            stream.WriteVInt(1);
            stream.WriteVInt(-1);
            stream.WriteVInt(2);
            stream.WriteVInt(1);
            stream.WriteVInt(4);

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 0xCC;
        }
    }
}
