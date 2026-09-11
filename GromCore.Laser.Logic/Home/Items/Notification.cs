using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Message.Club;
using GromCore.Laser.Logic.Notification;
using GromCore.Laser.Titan.DataStream;
using System.Net.Mail;
using System.Runtime;

namespace GromCore.Laser.Logic.Home.Items
{
    public class Notification
    {
        public int Id;
        public int SpecialId;
        // 1xxx - тексовый айди
        // 4xxx - ранговый айди
        // 2xxx - баннер айди
        public int Index;
        public bool IsViewed;
        public int TimePassed;
        public string MessageEntry;
        public string PrimaryMessageEntry;
        public string SecondaryMessageEntry;
        public string ButtonMessageEntry;
        public string FileLocation;
        public string FileSha;
        public string ExtLint;
        public List<int> HeroesIds;
        public List<int> HeroesTrophies;
        public List<int> HeroesTrophiesReseted;
        public List<int> StarpointsAwarded;
        public int DonationCount;
        public int SkinID;
        public int BrawlerID;
        public int ResourceID;
        public int ResourceCount;
        public int RevokePersonHighID;
        public int RevokePersonLowID;
        public int RevokeCount;
        public int VanityRewardGlobalId;
        public List<int> VanityRewardsGlobalId;
        public int AttachedAllianceId;
        public string Name;
        public PlayerDisplayData BandDisplayData;
        public DateTime DateTime;
        public int _season;
        public GemOffer gemOffer;
        public int ChallengeType;
        public string ChallengeName;
        public int Victory;
        public GemOffer alternativeGemOffer;

        public int PigSupply;
        public List<GemOffer> PigRewards=[];
        public int PigPlace;
        public List<GemOffer> PigPlaceReward =[];
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Id);
            stream.WriteInt(Index);
            stream.WriteBoolean(IsViewed);
            stream.WriteInt(0);
            stream.WriteString(MessageEntry);// base ||  FloaterTextNotification

            switch (Id)
            {
                case 2: // DonateNotification
                    stream.WriteString(null);
                    break;
                case 44:
                    stream.WriteVInt(0);
                    stream.WriteVInt(1);
                    stream.WriteVLong(1);
                    stream.WriteVInt(PigSupply); 
                    stream.WriteVInt(PigRewards.Count);
                    if (PigRewards.Count > 0)
                        foreach (var i in PigRewards)
                            i.Encode(stream);
                    stream.WriteVInt(PigPlace);
                    stream.WriteVInt(PigPlaceReward.Count);
                    if(PigPlaceReward.Count>0)
                        foreach (var i in PigRewards)
                            i.Encode(stream);
                    break;
                case 45: // RankedRankUpRewardNotification
                    stream.WriteVInt(1);  
                    stream.WriteBoolean(true); // true
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteBoolean(true); // gem offer
                    gemOffer.Encode(stream);
                    break;
                case 54:
                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    stream.WriteVInt(1); // on/off

                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    break;
                case 63: // ChallengeRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(1);
                    if(gemOffer != null) gemOffer.Encode(stream);
                    else new GemOffer(1,1,0,0).Encode(stream);
                    stream.WriteVInt(ChallengeType);
                    stream.WriteVInt(Victory);
                    stream.WriteString(ChallengeName);
                    if (stream.WriteBoolean(false))
                    {
                        stream.WriteVInt(1);
                        alternativeGemOffer?.Encode(stream);
                    }
                    break;
                case 64: // BoxRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    break;
                case 66: // FloaterTextNotification
                    break;
                case 67: // RankedMidSeasonRewardNotification
                    stream.WriteVInt(1); // count
                    stream.WriteBoolean(true); // true
                    stream.WriteBoolean(true); // true
                    stream.WriteVInt(0);
                    stream.WriteVInt(99); // condition
                    stream.WriteBoolean(true); // gem offer
                    new GemOffer(1, 1, 0, 0).Encode(stream);
                    break; // todo
                case 68: // RankedSeasonEndNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    if(false)
                        stream.WriteBoolean(false);
                    stream.WriteBoolean(true);
                    stream.WriteVInt(1); // id
                    stream.WriteDataReference(0, 0); // character id
                    stream.WriteVInt(1); // skin id
                    stream.WriteVInt(1); // global id
                    break;
                case 69: // BrawlPassAutoCollectSeasonNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(_season); // season -1 !!!
                    break;
                case 70: // ChallengeRewardNotification
                    break;
                case 71: // BrawlPassPointRewardNotification
                    break;
                case 72: // VanityItemRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(VanityRewardGlobalId);
                    break;
                case 73: // BrawlPassRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    break;
                case 75: // ChallengeSkinRewardNotification
                    stream.WriteVInt(0);
                    break;
                case 76: // QualifyNotification
                    break;
                case 77: // ProLeagueSeasonEndNotification
                    break;
                case 78: // RankRewardNotification
                    break;
                case 79: // StarPointsNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(HeroesIds.Count);
                    for (int i = 0; i < HeroesIds.Count; i++)
                    {
                        stream.WriteVInt(HeroesIds[i]);
                        stream.WriteVInt(HeroesTrophies[i]);
                        stream.WriteVInt(HeroesTrophiesReseted[i]);
                        stream.WriteVInt(StarpointsAwarded[i]);
                    }
                    stream.WriteVInt(0);
                    break;
                case 80: // ResourceRewardNotification
                    stream.WriteVInt(ResourceCount);
                    break;
                case 82: // BandNotification
                    stream.WriteVInt(1);
                    BandDisplayData.Encode(stream);
                    stream.WriteBoolean(true);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteBoolean(true);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteString("");
                    stream.WriteVInt(0);
                    break;

                case 81:
                    // stream.WriteVInt(0);
                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    break;
                case 83: // PromoPopupNotification
                    stream.WriteInt(0);
                    stream.WriteStringReference(PrimaryMessageEntry);
                    stream.WriteInt(0);
                    stream.WriteStringReference(SecondaryMessageEntry);
                    stream.WriteInt(0);
                    stream.WriteStringReference(ButtonMessageEntry);
                    stream.WriteStringReference(FileLocation);
                    stream.WriteStringReference(FileSha);
                    stream.WriteStringReference(ExtLint);
                    break;
                case 84: // StarPowerRewardNotification
                    break;
                case 85: // RevokeNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0); // type revork
                    stream.WriteVInt(RevokeCount);
                    stream.WriteInt(RevokePersonHighID);
                    stream.WriteInt(RevokePersonLowID);
                    stream.WriteVInt(0);
                    stream.WriteString("");
                    break;
                case 86: // IAPDeliveryNotification
                    break;
                case 88: // CoinDoublerRewardNotification
                    break;
                case 89: // GemRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(DonationCount);
                    break;
                case 90: // ResourceRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(5000000 + ResourceID);
                    stream.WriteVInt(ResourceCount);
                    break;
                case 93: // HeroRewardNotification
                    stream.WriteVInt(0); // mb lvl
                    stream.WriteVInt(0); // mb lvl
                    stream.WriteVInt(16000000 + BrawlerID);
                    break;
                case 94: // SkinRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(29000000 + SkinID);
                    break;
                default: // FreeTextNotification
                    stream.WriteVInt(0); 
                    break;
            }
            
        }
    }
}