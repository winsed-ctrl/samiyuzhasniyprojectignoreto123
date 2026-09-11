namespace GromCore.Laser.Logic.Message.Club
{
    using GromCore.Laser.Logic.Club;

    public class AllianceListMessage : GameMessage
    {
        private string SearchString;
        private List<Alliance> Alliances = new List<Alliance>();

        public void AddHeader(Alliance club)
        {
            Alliances.Add(club);
        }
        public void SetAlliances(Alliance[] alliances)
        {
            Alliances = alliances.ToList();
        }

        public void SetSearchString(string searchString)
        {
            SearchString = searchString;
        }

        public override void Encode()
        {
            Stream.WriteString(SearchString);
            Stream.WriteVInt(Alliances.Count);
            if (Alliances.Count > 0)
            {
                foreach (Alliance Header in Alliances)
                {
                    Header.Header.Encode(Stream);
                }
            }


        }

        public override int GetMessageType()
        {
            return 24310;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
