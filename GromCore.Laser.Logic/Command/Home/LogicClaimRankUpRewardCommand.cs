namespace GromCore.Laser.Logic.Command.Home
{
    using Newtonsoft.Json.Serialization;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using System.Security.Principal;

    public class LogicClaimRankUpRewardCommand : Command
    {
        public int MilestoneId { get; set; }
        public int UnknownDataId { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
        public int RequieredMilestone { get; set; }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            MilestoneId = stream.ReadVInt();
            UnknownDataId = ByteStreamHelper.ReadDataReference(stream);
            Unk2 = stream.ReadVInt();
            RequieredMilestone = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (!LogicServerListener.Instance.IsDev())
            {
                Debugger.Print($"Claim rankup reward: milestone: {MilestoneId}, data: {UnknownDataId}, unk2: {Unk2}, unk3: {Unk3}");

                if (MilestoneId == 6)
                {
                    string name = $"goal_6_{homeMode.Home.TrophyRoadProgress - 1}";


                    MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetData<MilestoneData>(name);
                    if (milestoneData == null)
                    {
                        Debugger.Error($"Milestone data is NULL - {name}");
                        return -111;
                    }

                    if (homeMode.Avatar.HighestTrophies < milestoneData.Progress + milestoneData.ProgressStart)
                    {
                        Debugger.Warning($"current progress: {homeMode.Avatar.HighestTrophies}, required progress: {milestoneData.Progress + milestoneData.ProgressStart}");
                        return -2;
                    }

                    if (ProcessReward(homeMode, milestoneData, false, 6, homeMode.Home.TrophyRoadProgress + 1, 0) != 0) return -1;

                    homeMode.Home.TrophyRoadProgress++;
                }
                else if (MilestoneId == 9 || MilestoneId == 10 || MilestoneId == 12)
                {
                    string name = $"Goal_{MilestoneId}_{homeMode.Home.BpSeason}_{(MilestoneId == 10 ? RequieredMilestone : RequieredMilestone)}";
                    if (MilestoneId == 9 && !homeMode.Home.HasPremiumPass)
                    {
                        return -1;
                    }
                    if (MilestoneId == 12 && !homeMode.Home.HasPremiumPassPlus)
                    {
                        return -1;
                    }

                    MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetData<MilestoneData>(name);
                    if (milestoneData == null)
                    {
                        Debugger.Error($"Milestone data is NULL - {name}");
                        return -1;
                    }

                    if (homeMode.Home.BrawlPassTokens < milestoneData.Progress + milestoneData.ProgressStart)
                    {
                        Debugger.Warning($"current progress: {homeMode.Home.BrawlPassTokens}, required progress: {milestoneData.Progress + milestoneData.ProgressStart}");
                        return -1;
                    }
                    if (MilestoneId == 9 && LogicBitHelper.Get(homeMode.Home.PremiumPassProgress, RequieredMilestone + 2))
                        return -1;
                    if (MilestoneId == 10 && LogicBitHelper.Get(homeMode.Home.BrawlPassProgress, RequieredMilestone + 2))
                        return -1;
                    if (MilestoneId == 12 && LogicBitHelper.Get(homeMode.Home.BrawlPassPlusProgress, RequieredMilestone + 2))
                        return -1;

                    if (ProcessReward(homeMode, milestoneData, false, MilestoneId, MilestoneId == 10 ? RequieredMilestone + 2 : RequieredMilestone + 2, homeMode.Home.BpSeason) != 0) return -1;

                    if (MilestoneId == 9)
                    {
                        homeMode.Home.PremiumPassProgress = LogicBitHelper.Set(homeMode.Home.PremiumPassProgress, RequieredMilestone + 2, true);
                    }
                    else if (MilestoneId == 10)
                    {
                        homeMode.Home.BrawlPassProgress = LogicBitHelper.Set(homeMode.Home.BrawlPassProgress, RequieredMilestone + 2, true);
                    }
                    else if (MilestoneId == 12)
                    {
                        homeMode.Home.BrawlPassPlusProgress = LogicBitHelper.Set(homeMode.Home.BrawlPassPlusProgress, RequieredMilestone + 2, true);
                    }
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                Debugger.Print($"Claim rankup reward: milestone: {MilestoneId}, data: {UnknownDataId}, unk2: {Unk2}, unk3: {Unk3}");

                if (MilestoneId == 6)
                {
                    string name = $"goal_6_{homeMode.Home.TrophyRoadProgress - 1}";


                    MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetData<MilestoneData>(name);
                    if (milestoneData == null)
                    {
                        Debugger.Error($"Milestone data is NULL - {name}");
                        return -111;
                    }

                    if (ProcessReward(homeMode, milestoneData, false, 6, homeMode.Home.TrophyRoadProgress + 1, 0) != 0) return 0;

                }
                else if (MilestoneId == 9 || MilestoneId == 10 || MilestoneId == 12)
                {
                    string name = $"Goal_{MilestoneId}_{homeMode.Home.BpSeason}_{(MilestoneId == 10 ? RequieredMilestone : RequieredMilestone)}";

                    MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetData<MilestoneData>(name);
                    if (milestoneData == null)
                    {
                        Debugger.Error($"Milestone data is NULL - {name}");
                        return -1;
                    }


                    if (ProcessReward(homeMode, milestoneData, false, MilestoneId, MilestoneId == 10 ? RequieredMilestone + 2 : RequieredMilestone + 2, homeMode.Home.BpSeason) != 0) return 0;

                }
                else
                {
                    return 0;
                }
            }

            return 0;
        }

        private int ProcessReward(HomeMode homeMode, MilestoneData milestoneData, bool useSecondaryReward, int track, int idx, int bpSeason)
        {
            int type = useSecondaryReward ? milestoneData.SecondaryLvlUpRewardType : milestoneData.PrimaryLvlUpRewardType;
            string data = !useSecondaryReward ? milestoneData.PrimaryLvlUpRewardData : milestoneData.SecondaryLvlUpRewardData;
            int count = useSecondaryReward ? milestoneData.SecondaryLvlUpRewardCount : milestoneData.PrimaryLvlUpRewardCount;
            Console.WriteLine(type);
            switch (type)
            {
                case 1:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(7);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 3:
                    {
                        CharacterData character = DataTables.Get(DataType.Character).GetData<CharacterData>(data);
                        if (homeMode.Avatar.HasHero(character.GetGlobalId()))
                        {
                            return ProcessReward(homeMode, milestoneData, true, track, idx, bpSeason);
                        }

                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(1);
                        drop.DataGlobalId = character.GetGlobalId();
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 6:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(10);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 9:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(10);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 4: // Skin
                    {
                        LogicGiveDeliveryItemsCommand command1 = new LogicGiveDeliveryItemsCommand();
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(9);
                        drop.SkinGlobalId = DataTables.Get(DataType.Skin).GetData<SkinData>(data).GetGlobalId();
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        SkinData skinData = DataTables.Get(DataType.Skin).GetData<SkinData>(data);
                        void NewCommand(SkinData skinData)
                        {
                            if (skinData == null) return;
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop reward = new GatchaDrop(9);
                            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                            {

                                if (emoteData.Skin == skinData.Name)
                                {
                                    GatchaDrop reward1 = new GatchaDrop(11);
                                    reward1.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
                                    reward1.Count = 1;
                                    unit.AddDrop(reward1);
                                }

                            }
                            foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
                            {
                                if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
                                {
                                    GatchaDrop reward1 = new GatchaDrop(11);
                                    reward1.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
                                    reward1.Count = 1;
                                    unit.AddDrop(reward1);
                                }
                            }

                            foreach (SprayData sprayData in DataTables.Get(DataType.Spray).GetDatas())
                            {
                                if (sprayData.Skin == skinData.Name)
                                {
                                    GatchaDrop reward1 = new GatchaDrop(11);
                                    reward1.DataGlobalId = DataTables.Get(DataType.Spray).GetData<SprayData>(sprayData.Name).GetGlobalId();
                                    reward1.Count = 1;
                                    unit.AddDrop(reward1);
                                }
                            }
                            command.DeliveryUnits.Add(unit);
                            if (command.DeliveryUnits.Count <= 0) return;
                            command.Execute(homeMode);

                            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                            message.Command = command;
                            homeMode.GameListener.SendMessage(message);

                        }

                        command1.DeliveryUnits.Add(unit);
                        command1.RewardTrackType = track;
                        command1.RewardForRank = idx;
                        command1.BrawlPassSeason = bpSeason;
                        command1.BrawlPassExecute = true;
                        command1.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command1;
                        homeMode.GameListener.SendMessage(message);
                        if (drop.SkinGlobalId != 29000384) NewCommand(skinData);
                    }
                    break;
                case 10:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(11);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 13:
                    break;
                case 12:
                    {
                        CharacterData character = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(UnknownDataId);

                        if (character == null)
                        {
                            return ProcessReward(homeMode, milestoneData, true, track, idx, bpSeason);
                        }

                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(6);
                        drop.DataGlobalId = character.GetGlobalId();
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 14:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(12);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 16:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(8);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 18: // Quests unlocked!
                    {
                        homeMode.Home.Quests = new Quests();
                        homeMode.Home.Quests.AddRandomQuests(homeMode.Avatar.Heroes, homeMode.Home.HasPremiumPass);

                        LogicHeroWinQuestsChangedCommand cmd = new LogicHeroWinQuestsChangedCommand();
                        cmd.Quests = homeMode.Home.Quests;

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = cmd;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 19: // Pins
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(data).GetGlobalId();
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                    }
                    break;
                case 25:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(data).GetGlobalId();
                        
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 35: // spray
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.Spray).GetData<SprayData>(data).GetGlobalId();
                        
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 38:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(22);
                        drop.Count = count;
                        unit.AddDrop(drop);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 41:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(24);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 43:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.Titul).GetData<TitlesData>(data).GetGlobalId();
                        
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 44: // Dayli Quest
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(8);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 45: // Blings
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(25);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 37: // Sprays unlocked!
                    {
                        break;
                    }
                case 29: // Clubs unlocked!
                    {
                        homeMode.GameListener.SendMessage(new AuthenticationFailedMessage() { ErrorCode = 1, Message = "Please rejoin." });
                        break;
                    }

                case 49: // Stardrop
                    {

                        homeMode.Home.DropsCount += 1;
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.Execute(homeMode);
                        homeMode.GameListener.SendCommand(command);

                        homeMode.Home.StarrDrop.GenerateRarityDrop(homeMode, (StarrDropRarity)milestoneData.PrimaryLvlUpRewardExtraData - 1);
                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();
                        logicRefreshRandomRewardsCommand.LogicRarity = milestoneData.PrimaryLvlUpRewardExtraData - 1;
                        logicRefreshRandomRewardsCommand.Execute(homeMode);
                        homeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
                        break;
                    }
                case 50: // Stardrop
                    {
                        homeMode.Home.DropsCount += 1;
                        LogicGiveDeliveryItemsCommand command = new();
                        DeliveryUnit unit = new DeliveryUnit(100);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.Execute(homeMode);

                        homeMode.GameListener.SendCommand(command);

                        homeMode.Home.StarrDrop.GenerateDrop(homeMode);
                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new();
                        logicRefreshRandomRewardsCommand.Execute(homeMode);
                        homeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
                        break;
                    }
                case 51: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(22);
                        drop.Count = 1000;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 93: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(15);
                        homeMode.SimulateGatcha(unit);
                        command.Execute(homeMode);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 94: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(20);
                        homeMode.SimulateGatcha(unit);
                        command.Execute(homeMode);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 95: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(16);
                        homeMode.SimulateGatcha(unit);
                        command.Execute(homeMode);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 96: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(17);
                        homeMode.SimulateGatcha(unit);
                        command.Execute(homeMode);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 91: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(18);
                        homeMode.SimulateGatcha(unit);
                        command.Execute(homeMode);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 92: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(19);
                        homeMode.SimulateGatcha(unit);
                        command.Execute(homeMode);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                default:
                    {
                        Debugger.Error("Unknown reward type: " + type);
                        return -3;
                    }
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 517;
        }
    }
}
