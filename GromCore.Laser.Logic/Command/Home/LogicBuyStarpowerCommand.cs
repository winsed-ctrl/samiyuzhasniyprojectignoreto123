namespace GromCore.Laser.Logic.Command.Home
{
    using Microsoft.Win32.SafeHandles;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Titan.DataStream;

    public class LogicBuyStarpowerCommand : Command
    {
        private int ID;
        
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            ID = stream.ReadVInt(); 
            stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            bool Check(int count)
            {
                if (count > homeMode.Avatar.Gold)
                {
                    homeMode.Avatar.Gold = 0;
                    int deficit = count - homeMode.Avatar.Gold;
                    if (!homeMode.Avatar.UseDiamonds((int)Math.Ceiling(deficit * GeneralStaticLogic.GEMS_PER_COIN)))
                    {
                        return false;
                    }
                    return true;
                }
                homeMode.Avatar.Gold -= count;
                return true;
            }

            CardData spg = DataTables.Get(23).GetData<CardData>(ID);
            // if (!GeneralStaticLogic.AllowedGadgets.Contains(ID))
            // {
            //     AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //     loginFailed.ErrorCode = 1;
            //     loginFailed.Message = "This gadget is unfinished.\nPlease try again later.";
            //     homeMode.GameListener.SendMessage(loginFailed);
            //     return -1;
            // }
            // if (!GeneralStaticLogic.AllowedSPGs.Contains(ID))
            // {
            //     AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //     loginFailed.ErrorCode = 1;
            //     loginFailed.Message = "This starpower is unfinished.\nPlease try again later.";
            //     homeMode.GameListener.SendMessage(loginFailed);
            //     return -1;
            // }
            // if (!GeneralStaticLogic.AllowedOvercharges.Contains(ID))
            // {
            //     AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //     loginFailed.ErrorCode = 1;
            //     loginFailed.Message = "This overcharge is unfinished.\nPlease try again later.";
            //     homeMode.GameListener.SendMessage(loginFailed);
            //     return -1;
            // }
            Console.WriteLine("IDK!!");
            if (!GeneralStaticLogic.AllowedGadgets.Contains(ID) && !GeneralStaticLogic.AllowedSPGs.Contains(ID) && !GeneralStaticLogic.AllowedOvercharges.Contains(ID))
            {
                Console.WriteLine("IDK!");
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "This skill is unfinished.\nPlease try again later.";
                homeMode.GameListener.SendMessage(loginFailed);
                return -1;
            }
            
            // Console.WriteLine("ID = " + ID);
            // Console.WriteLine("!GeneralStaticLogic.AllowedUnquies.Contains(ID) = " + !GeneralStaticLogic.AllowedUnquies.Contains(ID));
            // if (!GeneralStaticLogic.AllowedUnquies.Contains(ID)&& !LogicServerListener.Instance.IsDev())
            //     {
            //         AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //         loginFailed.ErrorCode = 1;
            //         loginFailed.Message = "This skill is unfinished.\nPlease try again later.";
            //         homeMode.GameListener.SendMessage(loginFailed);
            //         return -1;
            //     }
            // if (!LogicServerListener.Instance.IsDev())
            // {
            //     if (LogicServerListener.Instance.GetIntListFromField("Overcharge") == null
            //         || LogicServerListener.Instance.GetIntListFromField("SPG") == null
            //         || LogicServerListener.Instance.GetIntListFromField("Gadget") == null)
            //     {
            //         AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //         loginFailed.ErrorCode = 1;
            //         loginFailed.Message = "Unable connect to server\nPlease try again later.";
            //         homeMode.GameListener.SendMessage(loginFailed);
            //         return -1;
            //     }
            //     if (spg == null || spg.Name == null || spg.DirectPurchasePrice == 0) return -1;
            //     else if (LogicServerListener.Instance.GetIntListFromField("Overcharge").Contains(ID))
            //     {
            //         AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //         loginFailed.ErrorCode = 1;
            //         loginFailed.Message = "This overcharge is unfinished.\nPlease try again later.";
            //         homeMode.GameListener.SendMessage(loginFailed);
            //         return -1;
            //     }
            //     else if (LogicServerListener.Instance.GetIntListFromField("Gadget").Contains(ID))
            //     {
            //         AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //         loginFailed.ErrorCode = 1;
            //         loginFailed.Message = "This gadget is unfinished.\nPlease try again later.";
            //         homeMode.GameListener.SendMessage(loginFailed);
            //         return -1;
            //     }
            //     else if (LogicServerListener.Instance.GetIntListFromField("SPG").Contains(ID))
            //     {
            //         AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //         loginFailed.ErrorCode = 1;
            //         loginFailed.Message = "This starpower is unfinished.\nPlease try again later.";
            //         homeMode.GameListener.SendMessage(loginFailed);
            //         return -1;
            //     }
            // }
            homeMode.Avatar.SPGS.Add(spg.GetGlobalId());

            CharacterData hero;
            CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(29, ID));
            if (card == null) return -1;

            string m = card.Name.Replace("_2", "");
            m = m.Replace("_3", "");
            CardData card1 = DataTables.Get(DataType.Card).GetData<CardData>(m);
            CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_2");
            CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_3");
            hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
            if (hero == null) return -1;
            Hero? h = homeMode.Avatar.GetHero(hero.GetGlobalId());

            homeMode.Avatar.SelectedSPGS.Remove(card1.GetGlobalId());
            if(card2 != null)homeMode.Avatar.SelectedSPGS.Remove(card2.GetGlobalId());
            if (card3 != null)
            {
                homeMode.Avatar.SelectedSPGS.Remove(card3.GetGlobalId());
            }

            Hero playerHero = homeMode.Avatar.GetHeroForCard(card);
            if (spg.MetaType == 4)
            {
                playerHero.SelectedStarPowerId = spg.GetInstanceId();
                playerHero.ChampieSelectedStarPowerId = playerHero.SelectedStarPowerId;
            }
            else
            {
                playerHero.SelectedGadgetId = spg.GetInstanceId();
                playerHero.ChampieSelectedGadgetId = playerHero.SelectedGadgetId;
            }
           
            homeMode.Avatar.SelectedSPGS.Add(spg.GetGlobalId());
            homeMode.CharacterChanged.Invoke(0);
            
            Check(spg.DirectPurchasePrice);
            CharacterData boetc = DataTables.Get(16).GetData<CharacterData>(spg.Name.IndexOf('_') >= 0 ? spg.Name.Substring(0, spg.Name.IndexOf('_')) : spg.Name);
            if (homeMode.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(homeMode.Avatar.TeamId);
            return 0;
        }

        public override int GetCommandType()
        {
            return 557;
        }
    }
}
