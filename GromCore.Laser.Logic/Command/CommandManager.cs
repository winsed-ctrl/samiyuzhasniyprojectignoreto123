namespace GromCore.Laser.Logic.Command
{
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class CommandManager
    {
        private static Dictionary<int, Type> CommandTypes;

        static CommandManager()
        {

            CommandTypes = new Dictionary<int, Type>()
            {
                {557, typeof(LogicBuyStarpowerCommand) },
                {500, typeof(LogicGatchaCommand)},
                {505, typeof(LogicSetPlayerThumbnailCommand)},
                {506, typeof(LogicSelectSkinCommand)},
                //514 LogicDeleteNotificationCommand
                {515, typeof(LogicClearShopTickersCommand)},
                {517, typeof(LogicClaimRankUpRewardCommand)},
                {519, typeof(LogicPurchaseOfferCommand)},
                {520, typeof(LogicLevelUpCommand)},
                {521, typeof(LogicPurchaseHeroLvlUpMaterialCommand)},
                {522,typeof(LogicHeroSeenCommand) },
                {525, typeof(LogicSelectCharacterCommand)},
                {527, typeof(LogicSetPlayerNameColorCommand)},
                {529, typeof(LogicSelectStarPowerCommand)},
                {533, typeof(LogicQuestsSeenCommand)},
                {534, typeof(LogicPurchaseBrawlPassCommand)},
                {535, typeof(LogicClaimTailRewardCommand)},
                {536, typeof(LogicPurchaseBrawlPassProgressCommand)},
                {538, typeof(LogicSelectEmoteCommand)},
                {543, typeof(LogicSelectGearCommand)},
                {567, typeof(LogicEditBattlePassCommand1)},
                {568, typeof(LogicEditBattlePassCommand)},
                {570, typeof(LogicSelectFavouriteBrawlerCommand)},
                {571, typeof(LogicOpenRandomCommand)},
                {569, typeof(LogicClaimMasteriesCommand) },
                {560, typeof(LogicPurchaseBrawlerCommand) },
                {562, typeof(LogicStarRoadRewardCommand) },
                {558, typeof(LogicPurchaseGearCommand) },
                {550, typeof(LogicSetDailyRewardType) },
                {204, typeof(LogicDayChangedCommand) },
                {221, typeof(LogicTeamChatMuteStateChangedComamnd) },
                {555, typeof(LogicSelectSprayCommand) },
                {509, typeof(LogicPurchaseTokenDoublersCommand) },
                {528, typeof(LogicViewInboxNotificationCommand) },
                {540, typeof(LogicPurchaseChallengeLivesCommand) },
                // {563, typeof(LogicRecruitRoadProcessExtraRecruitTokensCommand) },
                //{562,typeof(LogicRecruitRoadAbandonNewBrawlerCommand) },
                //{559,typeof(LogicRecruitRoadSelectNewBrawlerCommand) },
                // {561, typeof(LogicRecruitRoadSwitchBrawlerCommand) },
                {554, typeof(LogicRerollQuestCommand) }
            };
        }

        public static Command DecodeCommand(ByteStream stream, int type)
        {
            Command command = CommandManager.CreateCommand(type);
            if (command == null)
            {
                Debugger.Warning("Command is unhandled: " + type);
                return null;
            }

            command.Decode(stream);
            return command;
        }

        public static Command CreateCommand(int type)
        {
            if (GeneralStaticLogic.EnableMessageDebbuging) Console.WriteLine($"Received Command: {type}");
            if (CommandTypes.ContainsKey(type))
            {
                return (Command)Activator.CreateInstance(CommandTypes[type]);
            }
            else
            {
                Debugger.Warning("Command is unhandled: " + type);
            }
            return null;
        }
    }
}
