namespace GromCore.Laser.Logic.Battle.Objects
{
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public static class GameObjectFactory//+32 type 64 encode 48IsAlive
    {
        public static Character CreateGameObjectByData(CharacterData characterData)
        {
            return new Character(characterData);
        }
        public static Projectile CreateGameObjectByData(ProjectileData characterData)
        {
            return new Projectile(characterData);
        }
        public static Item CreateGameObjectByData(ItemData characterData)
        {
            return new Item(characterData);
        }
        public static AreaEffect CreateGameObjectByData(AreaEffectData characterData)
        {
            return new AreaEffect(characterData);
        }
    }
}
