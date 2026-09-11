using System;
using System.Collections.Generic;
using GromCore.Laser.Logic.Home;

namespace GromCore.Laser.Logic
{
    public static class OfferBridge
    {
        public static List<CustomOfferData> GetCustomOffers()
        {
            // Этот метод будет заполняться из Server.Database
            return _customOffers;
        }
        
        public static void SetCustomOffers(List<CustomOfferData> offers)
        {
            _customOffers = offers;
        }
        
        private static List<CustomOfferData> _customOffers = new List<CustomOfferData>();
    }
    
    public class CustomOfferData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public int OldCost { get; set; }
        public int Currency { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string BackgroundName { get; set; }
        public bool IsActive { get; set; }
        public List<CustomRewardData> Rewards { get; set; }
    }
    
    public class CustomRewardData
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public int DataId { get; set; }
    }
}