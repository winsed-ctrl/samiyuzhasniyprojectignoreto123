namespace GromCore.Laser.Server.Logic.Game
{
    using GromCore.Laser.Logic.Battle;
    using GromCore.Laser.Logic.Battle.Structures;
    using System;
    using System.Collections.Concurrent;

    public static class Battles
    {
        private static long m_battleIdCounter;
        public static ConcurrentDictionary<long, BattleMode> m_battles;
        private static readonly Random _random = new Random();

        public static void Init()
        {
            m_battles = new ConcurrentDictionary<long, BattleMode>();
            m_battleIdCounter = 0;

            new Thread(Update).Start();
        }

        public static void Update()
        {
            while (true)
            {
                foreach (BattleMode battle in m_battles.Values.ToArray())
                {
                    if (battle.IsGameOver)
                    {
                        long batid = battle.Id;
                        m_battles.Remove(battle.Id, out _);
                        Logger.LogPrint($"Battle end! ID: {batid}");
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public static long Add(BattleMode battle)
        {
            long id = ++m_battleIdCounter;
            m_battles[id] = battle;
            Logger.LogPrint($"Battle started! ID: {id}");
            return id;
        }

        public static BattleMode Get(long id)
        {
            if (!m_battles.ContainsKey(id)) return null;
            return m_battles[id];
        }
        public static BattleMode GetRandom()
        {
            if (m_battles.Count == 0) return null;

            int randomIndex = _random.Next(m_battles.Count);
            return m_battles.ElementAt(randomIndex).Value;
        }
    }
}
