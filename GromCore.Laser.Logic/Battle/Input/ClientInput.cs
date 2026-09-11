namespace GromCore.Laser.Logic.Battle.Input
{
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Titan.Debug;
    public class ClientInput
    {
        public ClientInput()
        {
            ;
        }

        public int Index;
        public int Type;
        public int EmoteIndex;
        public int SprayIndex;
        public int SprayAngle;

        public int X, Y;

        public bool AutoAttack;
        public int AutoAttackTarget; // global id

        public long OwnerSessionId;

        public void Decode(BitStream Stream)
        {
            Index = Stream.ReadPositiveInt(15);
            
            //Debugger.Print(Index.ToString());
            Type = Stream.ReadPositiveInt(5);
            X = Stream.ReadInt(15);
            Y = Stream.ReadInt(15);
            Stream.ReadBoolean();
            AutoAttack = Stream.ReadBoolean();
            Stream.ReadBoolean();
            if (Type == 9) // use emote
            {
                EmoteIndex=Stream.ReadPositiveInt(3); // emote index
            }
            if (Type == 11) // use emote
            {
                Stream.ReadPositiveVIntMax255(); // emote index
            }
            if (Type == 15) // ����i
            {
                SprayIndex = Stream.ReadPositiveIntMax511()-6; // Spray Index
                SprayAngle =  Stream.ReadPositiveIntMax511(); // Spray Angle
            }
            if (Type == 16) // ����� �����
            {
                Stream.ReadPositiveIntMax511(); 
                //Stream.ReadPositiveIntMax511(); 
            }
            if (AutoAttack)
            {
                if (Stream.ReadBoolean())
                {
                    int v7 = Stream.ReadPositiveInt(14); // global id
                    // Console.WriteLine("v7 = " + v7);
                    AutoAttackTarget = GlobalId.CreateGlobalId(1, v7);
                }
            }
        }
    }
}
