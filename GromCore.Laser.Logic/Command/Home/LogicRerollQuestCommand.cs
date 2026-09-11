namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public class LogicRerollQuestCommand : Command
    {
        private int _rerollId;
        public override int Execute(HomeMode homeMode)
        {
            if (LogicServerListener.Instance.IsDev()) Console.WriteLine($"Reroll Quest Id: {_rerollId}");
            if (homeMode.Home.UsedRerolls <= 0 && !LogicServerListener.Instance.IsDev()) return -1;
            Quest _target = null;
            if (homeMode.Home.Quests.QuestList.Find(q => q.Id == _rerollId) == null) return -1;
            _target = homeMode.Home.Quests.QuestList.Find(q => q.Id == _rerollId);
            
           
            Quest _new = homeMode.Home.Quests.GenerateQuest(homeMode.Avatar.Heroes, _target.QuestType == 1, _target.NeedPass);
            _new.Id = _target.Id;
            _new.UsedRefresh = true;
            homeMode.Home.Quests.QuestList.Add(_new);
            homeMode.GameListener.SendCommand(new LogicHeroWinQuestsChangedCommand { Quests = homeMode.Home.Quests, usedrerolls = homeMode.Home.UsedRerolls, seas = homeMode.Home.BpSeason });
            homeMode.Home.Quests.QuestList.Remove(_target);
            homeMode.Home.UsedRerolls-=1;
            
            if(homeMode.Home.Quests.QuestList.Find(q => q.Id == _rerollId) == null) return -1;
            homeMode.Home.Quests.QuestList.Find(q => q.Id == _rerollId).UsedRefresh = false;
            return 0;
        }
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            _rerollId = stream.ReadVInt();
        }
        public override int GetCommandType()
        {
            return 554;
        }
    }
}
