namespace GromCore.Laser.Server.Logic.Game
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Titan.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Server.Networking.Session;
    using EventData = GromCore.Laser.Logic.Home.Items.EventData;
    using System.Configuration;

    public static class Events
    {
        public const int REFRESH_MINUTES = 120;

        private static Timer RefreshTimer;
        private static EventSlotConfig[] ConfigSlots;
        private static Dictionary<int, EventData> Slots;
        private static readonly object SlotsLock = new();

        private static DateTime GetMSKTimeToRefresh(string gameMode)
        {
            if (gameMode == "ispa") return DateTime.Now;
            int hour = gameMode switch
            {
                "Bounty" or "Heist" => 17,
                "Showdown" or "DuoShowdown" => 0,
                "GemGrab" => 12,
                "KingOfHill" => 22,
                "BigGame" => 0,
                "HoldTheBall" => 0,
                "BasketBrawl" => 0,
                "VolleyBrawl" => 0,
                "BrawlBall" => 0,
                "ProtectKing" => 0,
                "Payload" => 0,
                "Invasion" => 0,
                "TagTeam" => 0,
                "BossFight_TownCrush" => 0,
                "CTF" => 0,
                _ => 4
            };

            TimeZoneInfo mskZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            DateTime mskNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, mskZone);
            DateTime targetTimeToday = new DateTime(mskNow.Year, mskNow.Month, mskNow.Day, hour, 0, 0);

            if (mskNow >= targetTimeToday)
            {
                targetTimeToday = targetTimeToday.AddDays(1);
            }

            return targetTimeToday;
        }

        public static void Init()
        {
            LoadSettings();
            Slots = new Dictionary<int, EventData>();
            GenerateEvents();
            
            RefreshTimer = new Timer(CheckEventsRefresh, null, TimeSpan.Zero, TimeSpan.FromMinutes(0.1));
        }

        private static void CheckEventsRefresh(object state)
        {
            try
            {
                DateTime now = DateTime.UtcNow;

                foreach (var config in ConfigSlots)
                {
                    if (!Slots.TryGetValue(config.Slot, out var currentEvent))
                    {
                        RegenerateSlot(config);
                        continue;
                    }

                    if (currentEvent != null && now >= currentEvent.TimeToNext)
                    {
                        RegenerateSlot(config);
                    }
                    
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error refreshing events: {ex}");
            }
        }

        private static void GenerateEvents()
        {
            foreach (var config in ConfigSlots)
            {
                var eventData = GenerateEvent(config.AllowedModes, config.Slot, config.location, config.modifi, config);
                lock (SlotsLock)
                {
                    Slots[config.Slot] = eventData;
                }
            }
        }

        private static void RegenerateSlot(EventSlotConfig config)
        {
            if (config.IsChallengeSlot) return;
            var newEvent = GenerateEvent(config.AllowedModes, config.Slot, config.location, config.modifi, config);
            if (newEvent == null) return;

            lock (SlotsLock)
            {
                Slots[config.Slot] = newEvent;
            }
            Matchmaking.ReloadSlot(config.Slot, newEvent);
            NotifyPlayers();
        }

        private static void NotifyPlayers()
        {    
            foreach (var session in Sessions.ActiveSessions.Values.ToArray())
            {
                try
                {
                    session.Home.Home.Events = GetEvents();

                    var command = new LogicDayChangedCommand
                    {
                        Events = GetEvents()
                    };
                    
                    session.Connection.Send(new AvailableServerCommandMessage
                    {
                        Command = command
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private static EventData GenerateEvent(string[] gameModes, int slot, int presetlocation, HashSet<int> modifi, EventSlotConfig config = null)
        {
            LocationData location = null;
            if (config != null && config.IsChallengeSlot)
            {
                if (config.EndTime.HasValue && config.EndTime.Value <= DateTime.UtcNow)
                {
                    Console.WriteLine($"challenge in slot {slot} has expired (End Time: {config.EndTime.Value})");
                    return null;
                }
            }
            if (presetlocation != 0)
            {
                location = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(presetlocation);
                if (location == null)
                {
                    Console.WriteLine($"preset location {presetlocation} not found!");
                    return null;
                }
            }
            else
            {
                int count = DataTables.Get(DataType.Location).Count;
                Random rand = new Random();
                int tries = 0;
                while (tries < 1000 && location == null)
                {
                    var potentialLocation = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(rand.Next(0, count));
                    if (potentialLocation != null &&
                        !potentialLocation.Disabled &&
                        gameModes.Contains(potentialLocation.GameModeVariation))
                    {
                        location = potentialLocation;
                    }
                    tries++;
                }

                if (location == null)
                {
                    tries = 0;
                    while (tries < 1000 && location == null)
                    {
                        var potentialLocation = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(rand.Next(0, count));
                        if (potentialLocation != null &&
                            gameModes.Contains(potentialLocation.GameModeVariation))
                        {
                            location = potentialLocation;
                        }
                        tries++;
                    }
                }
            }

            if (location == null)
            {
                Console.WriteLine($" slot not found {slot}");
                return null;
            }

            DateTime refreshTime = GetMSKTimeToRefresh(config.IsChallengeSlot ? "ispa" : location.GameModeVariation);
            DateTime refreshTimeUTC = config.IsChallengeSlot ? DateTime.Now: TimeZoneInfo.ConvertTimeToUtc(refreshTime, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));

            var eventData = new EventData
            {
                Slot = slot,
                LocationId = location?.GetGlobalId() ?? 0, 
                EndTime = refreshTimeUTC,
                TimeToNext = refreshTimeUTC,
                modifi = modifi
            };
            if (config != null && config.IsChallengeSlot)
            {
                eventData.Title = config.Title;
                eventData.SubTitle = config.SubTitle;
                eventData.ChallengeLosePerStep = config.ChallengeLosePerStep;
                eventData.ChallengeType = config.ChallengeType;
                eventData.GemOfferType = config.GemOfferType;
                eventData.GemOfferCount = config.GemOfferCount;
                eventData.GemOfferData1 = config.GemOfferData1;
                eventData.GemOfferData2 = config.GemOfferData2;
                eventData.GemOfferExtra = config.GemOfferExtra;
                eventData.ExtraLives = config.ExtraLives ?? new List<int>();
                eventData.FileHash = config.FileHash;
                eventData.FileName = config.FileName;
                eventData.cgemofferlist = config.Rewards ?? new List<ChallengeGemOffer>();
                if (config.EndTime.HasValue)
                {
                    eventData.EndTime = config.EndTime.Value;
                    eventData.TimeToNext = config.EndTime.Value;
                }
            }

            return eventData;
        }

        private static void LoadSettings()
        {
            LogicJSONObject settings = LogicJSONParser.ParseObject(File.ReadAllText("gameplay.json"));
            LogicJSONArray slots = settings.GetJSONArray("slots");
            ConfigSlots = new EventSlotConfig[slots.Size()];

            for (int i = 0; i < slots.Size(); i++)
            {
                EventSlotConfig config = new EventSlotConfig();

                LogicJSONObject slot = slots.GetJSONObject(i);
                config.Slot = slot.GetJSONNumber("slot").GetIntValue();

                LogicJSONArray gameModes = slot.GetJSONArray("game_modes");
                LogicJSONArray modifi = slot.GetJSONArray("modificators");

                config.AllowedModes = new string[gameModes.Size()];
                config.location = slot.GetJSONNumber("location").GetIntValue();

                for (int j = 0; j < gameModes.Size(); j++)
                {
                    config.AllowedModes[j] = gameModes.GetJSONString(j).GetStringValue();
                }

                if (modifi != null)
                {
                    for (int k = 0; k < modifi.Size(); k++)
                    {
                        LogicJSONNumber numberNode = modifi.GetJSONNumber(k);
                        if (numberNode != null)
                            config.modifi.Add(numberNode.GetIntValue());
                    }
                }

                if (config.IsChallengeSlot)
                {
                    LogicJSONString titleNode = slot.GetJSONString("Title");
                    if (titleNode != null)
                        config.Title = titleNode.GetStringValue();

                    LogicJSONString subTitleNode = slot.GetJSONString("SubTitle");
                    if (subTitleNode != null)
                        config.SubTitle = subTitleNode.GetStringValue();

                    LogicJSONNumber challengeLosePerStepNode = slot.GetJSONNumber("ChallengeLosePerStep");
                    if (challengeLosePerStepNode != null)
                        config.ChallengeLosePerStep = challengeLosePerStepNode.GetIntValue();

                    LogicJSONNumber challengeTypeNode = slot.GetJSONNumber("ChallengeType");
                    if (challengeTypeNode != null)
                        config.ChallengeType = challengeTypeNode.GetIntValue();

                    LogicJSONNumber gemOfferTypeNode = slot.GetJSONNumber("GemOfferType");
                    if (gemOfferTypeNode != null)
                        config.GemOfferType = gemOfferTypeNode.GetIntValue();

                    LogicJSONNumber gemOfferCountNode = slot.GetJSONNumber("GemOfferCount");
                    if (gemOfferCountNode != null)
                        config.GemOfferCount = gemOfferCountNode.GetIntValue();

                    LogicJSONNumber gemOfferData1Node = slot.GetJSONNumber("GemOfferData1");
                    if (gemOfferData1Node != null)
                        config.GemOfferData1 = gemOfferData1Node.GetIntValue();

                    LogicJSONNumber gemOfferData2Node = slot.GetJSONNumber("GemOfferData2");
                    if (gemOfferData2Node != null)
                        config.GemOfferData2 = gemOfferData2Node.GetIntValue();

                    LogicJSONNumber gemOfferExtraNode = slot.GetJSONNumber("GemOfferExtra");
                    if (gemOfferExtraNode != null)
                        config.GemOfferExtra = gemOfferExtraNode.GetIntValue();

                    LogicJSONArray extraLivesArray = slot.GetJSONArray("ExtraLives");
                    if (extraLivesArray != null)
                    {
                        config.ExtraLives = new List<int>();
                        for (int l = 0; l < extraLivesArray.Size(); l++)
                        {
                            LogicJSONNumber liveNode = extraLivesArray.GetJSONNumber(l);
                            if (liveNode != null)
                                config.ExtraLives.Add(liveNode.GetIntValue());
                        }
                    }

                    LogicJSONString fileHashNode = slot.GetJSONString("FileHash");
                    if (fileHashNode != null)
                        config.FileHash = fileHashNode.GetStringValue();

                    LogicJSONString fileNameNode = slot.GetJSONString("FileName");
                    if (fileNameNode != null)
                        config.FileName = fileNameNode.GetStringValue();

                    LogicJSONString endTimeNode = slot.GetJSONString("EndTime");
                    if (endTimeNode != null && !string.IsNullOrEmpty(endTimeNode.GetStringValue()))
                    {
                        if (DateTime.TryParseExact(endTimeNode.GetStringValue(), "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsedEndTime))
                            config.EndTime = parsedEndTime;
                        else
                            Console.WriteLine($"WARNING: Invalid EndTime format for slot {config.Slot}: {endTimeNode.GetStringValue()}");
                    }

                    LogicJSONString startTimeNode = slot.GetJSONString("StartTime");
                    if (startTimeNode != null && !string.IsNullOrEmpty(startTimeNode.GetStringValue()))
                    {
                        if (DateTime.TryParseExact(startTimeNode.GetStringValue(), "yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsedEndTime))
                            config.StartTime = parsedEndTime;
                        else
                            Console.WriteLine($"WARNING: Invalid StartTime format for slot {config.Slot}: {endTimeNode.GetStringValue()}");
                    }

                    LogicJSONArray rewardsArray = slot.GetJSONArray("Rewards");
                    if (rewardsArray != null)
                    {
                        config.Rewards = new List<ChallengeGemOffer>();
                        for (int r = 0; r < rewardsArray.Size(); r++)
                        {
                            LogicJSONObject rewardObj = rewardsArray.GetJSONObject(r);
                            if (rewardObj != null)
                            {
                                ChallengeGemOffer reward = new ChallengeGemOffer();

                                LogicJSONNumber typeNode = rewardObj.GetJSONNumber("RewardGemOfferType");
                                if (typeNode != null)
                                    reward.RewardGemOfferType = typeNode.GetIntValue();

                                LogicJSONNumber countNode = rewardObj.GetJSONNumber("RewardGemOfferCount");
                                if (countNode != null)
                                    reward.RewardGemOfferCount = countNode.GetIntValue();

                                LogicJSONNumber data1Node = rewardObj.GetJSONNumber("RewardGemOfferData1");
                                if (data1Node != null)
                                    reward.RewardGemOfferData1 = data1Node.GetIntValue();

                                LogicJSONNumber data2Node = rewardObj.GetJSONNumber("RewardGemOfferData2");
                                if (data2Node != null)
                                    reward.RewardGemOfferData2 = data2Node.GetIntValue();

                                LogicJSONNumber extraNode = rewardObj.GetJSONNumber("RewardGemOfferExtra");
                                if (extraNode != null)
                                    reward.RewardGemOfferExtra = extraNode.GetIntValue();

                                config.Rewards.Add(reward);
                            }
                        }
                    }
                }

                ConfigSlots[i] = config;
            }
        }

        public static EventData GetEvent(int slot)
        {
            lock (SlotsLock)
            {
                return Slots != null && Slots.TryGetValue(slot, out var eventData) ? eventData : null;
            }
        }

        public static bool HasSlot(int slot)
        {
            lock (SlotsLock)
            {
                return Slots != null && Slots.ContainsKey(slot);
            }
        }

        public static EventData[] GetEvents()
        {
            lock (SlotsLock)
            {
                return Slots?.Values.Where(eventData => eventData != null).ToArray() ?? Array.Empty<EventData>();
            }
        }

        public static LocationData[] GetAvailableMaps(int slot)
        {
            EventData currentEvent = GetEvent(slot);
            if (currentEvent?.Location == null)
                return Array.Empty<LocationData>();

            string gameMode = currentEvent.Location.GameModeVariation;
            return DataTables.Get(DataType.Location).GetDatas()
                .OfType<LocationData>()
                .Where(location => !location.Disabled && location.GameModeVariation == gameMode)
                .OrderBy(location => location.GetName(), StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool TryChangeMap(int slot, int locationId, out EventData changedEvent, out string error)
        {
            changedEvent = null;
            error = null;

            if (locationId < 0)
            {
                error = "ID карты не может быть отрицательным.";
                return false;
            }

            if (locationId >= 1_000_000 && GlobalId.GetClassId(locationId) != (int)DataType.Location)
            {
                error = $"ID {locationId} не является ID карты (ожидается класс {(int)DataType.Location}).";
                return false;
            }

            int instanceId = GlobalId.GetInstanceId(locationId);
            DataTable locations = DataTables.Get(DataType.Location);
            LocationData newLocation = locations.GetData<LocationData>(instanceId);

            if (newLocation == null)
            {
                error = $"Карта с ID {locationId} не найдена в игровых данных.";
                return false;
            }

            if (newLocation.Disabled)
            {
                error = $"Карта {newLocation.GetName()} отключена в игровых данных.";
                return false;
            }

            lock (SlotsLock)
            {
                if (Slots == null || !Slots.TryGetValue(slot, out EventData currentEvent) || currentEvent?.Location == null)
                {
                    error = $"Активный слот {slot} не найден.";
                    return false;
                }

                if (currentEvent.IsChallengeSlot)
                {
                    error = "Карты испытаний нельзя менять этой командой.";
                    return false;
                }

                string currentMode = currentEvent.Location.GameModeVariation;
                if (!string.Equals(newLocation.GameModeVariation, currentMode, StringComparison.Ordinal))
                {
                    error = $"Карта относится к режиму {newLocation.GameModeVariation}, а слот {slot} — к режиму {currentMode}.";
                    return false;
                }

                currentEvent.LocationId = newLocation.GetGlobalId();
                changedEvent = currentEvent;
            }

            Matchmaking.ReloadSlot(slot, changedEvent);
            NotifyPlayers();
            return true;
        }

        private class EventSlotConfig
        {
            public int Slot { get; set; }
            public string[] AllowedModes { get; set; }
            public int location;
            public HashSet<int> modifi = new();

            public string Title;
            public string SubTitle;
            public int ChallengeLosePerStep;
            public int ChallengeType;
            public int GemOfferType;
            public int GemOfferCount;
            public int GemOfferData1;
            public int GemOfferData2;
            public int GemOfferExtra;
            public List<int> ExtraLives = new();
            public string FileHash;
            public string FileName;
            public List<ChallengeGemOffer> Rewards = new();
            public DateTime? EndTime;
            public DateTime? StartTime;
            public bool IsChallengeSlot => Slot >= 20 && Slot <= 24;
        }
        private class ServerEventData
        {
            public int Slot { get; set; }
            public int LocationId { get; set; }
            public DateTime EndTime { get; set; }
        }
    }
}
