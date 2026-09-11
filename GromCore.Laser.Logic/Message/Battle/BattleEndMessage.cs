namespace GromCore.Laser.Logic.Message.Battle
{
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Logic.Home.Structures;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Numerics;
    using System.Text;

    public class BattleEndMessage : GameMessage
    {
        public BattleEndMessage() : base()
        {
            ProgressiveQuests = new List<Quest>();
        }

        public int Result;
        public int TokensReward;
        public int TrophiesReward;
        public List<BattlePlayer> Players;
        public List<Quest> ProgressiveQuests;
        public BattlePlayer OwnPlayer;
        public bool StarToken;

        public int GameMode;

        public bool BattleWithoutTrophies;
        public bool IsPvP;

        public int Winstreak;
        public int WinstreakTrophies;
        public int MasteryGained;
        public int MilestoneId;
        public int TokenDoublers;
        public int TokenDoublersRemaining;

        public int Step = -1;
        public int var = -1;
        public GemOffer reward;
        public int tryes;
        public bool end;

        public int Plus;
        public int Minus;

        public bool RankedMatch;
        public int MatchType;
        public int Rank;
        public int UpRank;
        public int ELO;
        public int TrueBlueTeamWins;
        public int TrueRedTeamWins;
        public int Round;
        public bool Solo;
        public int Add;
        public override void Encode()
        {
            Stream.WriteLong(OwnPlayer.AccountId);
            Stream.WriteLong(OwnPlayer.AccountId);

            Stream.WriteVInt(GameMode); // game mode
            Stream.WriteVInt(Result);
            Stream.WriteVInt(TokensReward); // tokens reward
            Stream.WriteVInt(TrophiesReward); // trophies reward
            Stream.WriteVInt(0);//Power Play Points Gained (Pro League Points)
            Stream.WriteVInt(TokenDoublers);// сколько доб
            Stream.WriteVInt(0);//Double Token Event (Double Event Keys)
            Stream.WriteVInt(TokenDoublersRemaining);// осталось
            Stream.WriteVInt(0);//game Lenght In Seconds
            Stream.WriteVInt(0);//Epic Win Power Play Points Gained (op Win Points)
            Stream.WriteVInt(Step);//Championship Level Reached (CC Wins)

            if (Stream.WriteBoolean(reward != null))
                reward.Encode(Stream);

            Stream.WriteVInt(tryes);
            Stream.WriteVInt(3);

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(0); // lavina manetok
            Stream.WriteVInt(0); // chromalavina
            Stream.WriteVInt(0); // underdog
            Stream.WriteVInt(WinstreakTrophies); // WINSTREAK ++
            Stream.WriteVInt(Winstreak); // WINSTREAK
            Stream.WriteVInt(Plus);// trophies +
            Stream.WriteVInt(Minus);// trophies +
            Stream.WriteVInt(0);//v53

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false); // no experience
            Stream.WriteBoolean(false); // no tokens left
            //Stream.WriteBoolean(BattleWithoutTrophies); // Result type
            //Stream.WriteBoolean(IsPvP); // isPvp
            Stream.WriteBoolean(BattleWithoutTrophies); // Result type
            Stream.WriteBoolean(IsPvP); // isPvp
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false); // powerplay
            Stream.WriteBoolean(false);

            Stream.WriteVInt(var);//ChallengeType
            Stream.WriteBoolean(end); // if win champ потом я спатькороче

            Stream.WriteVInt(Players.Count);
            foreach (BattlePlayer player in Players)
            {
                Stream.WriteBoolean(player.AccountId == OwnPlayer.AccountId); // is own player
                Stream.WriteBoolean(player.TeamIndex != OwnPlayer.TeamIndex); // is enemy
                Stream.WriteBoolean(false); // Star player
                Stream.WriteByte(1);
                {
                    ByteStreamHelper.WriteDataReference(Stream, player.CharacterId);
                }
                Stream.WriteByte(1);
                {
                    ByteStreamHelper.WriteDataReference(Stream, player.SkinId);//skin
                }
                Stream.WriteByte(1);
                {
                    Stream.WriteVInt(player.Trophies);
                }
                Stream.WriteByte(1);
                {
                    Stream.WriteVInt(player.HeroPowerLevel);
                }
                Stream.WriteByte(1);
                {
                    Stream.WriteVInt(0);
                }

                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                if (Stream.WriteBoolean(player.IsBot() == 0))
                {
                    Stream.WriteLong(player.AccountId);
                }
                player.DisplayData.Encode(Stream);
                Stream.WriteBoolean(false);

                Hero hero = null;
                if (player != null && player.IsBot() < 1) hero = player.Avatar.GetHero(player.CharacterId);
                if (hero != null)
                {
                    Stream.WriteByte(1);
                    Stream.WriteVInt(hero.MasteryPoints);
                    Stream.WriteByte(1);
                    int mastery = MasteryGained;
                    if (hero.MasteryPoints >= GeneralStaticLogic.MaxMastery) mastery = -1;
                    Stream.WriteVInt(mastery);
                }
                else
                {
                    Stream.WriteByte(0);
                    Stream.WriteByte(0);
                }


                Stream.WriteShort((short)player.Kills);
                Stream.WriteShort((short)player.Deaths);
                Stream.WriteInt(player.Damage);
                Stream.WriteInt(player.Heals);
                Stream.WriteDataReference(player.Ti);

            }

            Stream.WriteVInt(0);
            Stream.WriteVInt(MilestoneId > 0 ? 1 : 0);
            if (MilestoneId > 0) Stream.WriteDataReference(39, MilestoneId);

            Stream.WriteVInt(1); // milestone progress  
            {
                Stream.WriteVInt(1);
                Stream.WriteVInt(OwnPlayer.Trophies); // Trophies
                Stream.WriteVInt(OwnPlayer.HighestTrophies); // Highest Trophies

                //Stream.WriteVInt(5);
                //Stream.WriteVInt(100);
                //Stream.WriteVInt(100);
            }

            Stream.WriteVInt(0);//dataref
            //ByteStreamHelper.WriteDataReference(Stream, OwnPlayer.Home.Thumbnail);

            if (Stream.WriteBoolean(!RankedMatch))//play again
            {
                Stream.WriteInt(3);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);

                Stream.WriteInt(0);
                Stream.WriteInt(0);


            }
            if (Stream.WriteBoolean(true)) //quests
            {
                Stream.WriteVInt(ProgressiveQuests.Count);
                foreach (Quest quest in ProgressiveQuests)
                {
                    quest.EncodeHome(Stream);
                }
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);

            }
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);//v53
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);//v53
            if (Stream.WriteBoolean(RankedMatch))
            {
                Stream.WriteVInt(6);
                Stream.WriteVInt(Solo ? 0 : 1); // matchtype
                Stream.WriteVInt(ELO);
                Stream.WriteVInt(Rank); // rank to
                Stream.WriteVInt(ELO);
                Stream.WriteVInt(UpRank); // new rank
                Stream.WriteVInt(ELO); // points
                Stream.WriteVInt(TrueBlueTeamWins); // win true blue team
                Stream.WriteVInt(TrueRedTeamWins); // win true red team
                Stream.WriteVInt(Round); // next round
                Stream.WriteVInt(10); // time 

                Stream.WriteVInt(0); // quest later bru
                //if (Stream.WriteBoolean(false))
                //{
                //    if (Stream.WriteBoolean(true))
                //    {
                //        Stream.WriteVInt(1);
                //        Stream.WriteVInt(1);
                //    }

                //    Stream.WriteBoolean(true);
                //    new GemOffer(1, 1, 0, 0).Encode(Stream);
                //}
                Stream.WriteVInt(0);
                //Stream.WriteBoolean(false);

                Stream.WriteVInt(0);
                //if (Stream.WriteBoolean(true))
                //{
                //    if (Stream.WriteBoolean(true))
                //    {
                //        Stream.WriteVInt(1); // total
                //        Stream.WriteVInt(999); // goal
                //    }
                //
                //    Stream.WriteBoolean(true);
                //    new GemOffer(16, 1, 0, 0).Encode(Stream);
                // }
                Stream.WriteVInt(0);
                //Stream.WriteBoolean(false);

                Stream.WriteBoolean(false);
                //Stream.WriteVInt(1);
                //Stream.WriteVInt(1);

                Stream.WriteBoolean(false);
                //Stream.WriteVInt(1);
                //Stream.WriteVInt(1);

                Stream.WriteBoolean(false); // debug?
                                            //for(int i = 0; i < 9; i++)
                                            //{
                                            //    Stream.WriteVInt(i);
                                            //}

                Stream.WriteVInt(4); // round needed
                Stream.WriteVInt(0);

            }
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);//chronosTextEntry
            //{
            //    Stream.WriteInt(0);
            //    Stream.WriteString("507");
            //}
            Stream.WriteVInt(0);

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(RankedMatch);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            if (Stream.WriteBoolean(true))
            {
                Stream.WriteVInt(6);
                for(byte i = 0; i < 6; i++)
                {
                    Stream.WriteByte(i);
                    Stream.WriteByte(i);
                }
            }
            Stream.WriteBoolean(false);//v53
        }

        public override int GetMessageType()
        {
            return 23456;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}
