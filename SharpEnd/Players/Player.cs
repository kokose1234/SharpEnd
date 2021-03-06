﻿using MySql.Data.MySqlClient;
using SharpEnd.Data;
using SharpEnd.Drawing;
using SharpEnd.Maps;
using SharpEnd.Network;
using SharpEnd.Packets;
using SharpEnd.Servers;
using SharpEnd.Utility;

namespace SharpEnd.Players
{
    internal sealed class Player : MapEntity
    {
        public Client Client { get; private set; }

        public bool IsInitialized { get; private set; }

        public int Identifier { get; private set; }
        public string Name { get; private set; }
        public byte Gender { get; private set; }
        public byte Skin { get; private set; }
        public int Face { get; private set; }
        public int Hair { get; private set; }
        public int MapIdentifier { get; set; }
        public sbyte SpawnPoint { get; set; }
        public byte PortalCount { get; set; }

        public PlayerStats Stats { get; private set; }
        public PlayerItems Items { get; private set; }
        public PlayerSkills Skills { get; private set; }
        public PlayerQuests Quests { get; private set; }
        public PlayerSPTable SPTable { get; private set; }
        public ControlledMobs ControlledMobs { get; private set; }
        public ControlledNpcs ControlledNpcs { get; private set; }

        // TODO: Move else-where
        public bool IsGm => Client.Account.Level >= EAccountLevel.Gm;

        public override int ObjectIdentifier
        {
            get
            {
                return Identifier;
            }
            set
            {
                return;
            }
        }

        public Player(Client client, DatabaseQuery query)
        {
            Client = client;

            Identifier = query.Get<int>("identifier");
            Name = query.Get<string>("name");
            Gender = query.Get<byte>("gender");
            Skin = query.Get<byte>("skin");
            Face = query.Get<int>("face");
            Hair = query.Get<int>("hair");
            MapIdentifier = query.Get<int>("map_identifier");
            SpawnPoint = query.Get<sbyte>("map_spawn");

            if (IsGm)
            {
                MapIdentifier = 180000000;
                SpawnPoint = -1;
            }
            else if (false) // TODO: Check if the map has a forced return map
            {
                // TODO: Set map to forced return map
                SpawnPoint = -1;
            }
            else if (!Stats.IsAlive)
            {
                // TODO: Set map to return map
                SpawnPoint = -1;
            }

            Map = MasterServer.Instance.GetMaps(Client.ChannelIdentifier)[MapIdentifier];
            Position = MasterServer.Instance.GetMaps(Client.ChannelIdentifier)[MapIdentifier].Portals.GetSpawnPoint(SpawnPoint).Position;
            Stance = 0;
            Foothold = 0;

            Stats = new PlayerStats(this, query);

            using (DatabaseQuery itemQuery = Database.Query("SELECT * FROM player_item WHERE player_identifier=@player_identifier", new MySqlParameter("player_identifier", Identifier)))
            {
                long meso = query.Get<long>("meso");

                byte equipmentSlots = query.Get<byte>("equipment_slots");
                byte usableSlots = query.Get<byte>("usable_slots");
                byte setupSlots = query.Get<byte>("setup_slots");
                byte etceteraSlots = query.Get<byte>("etcetera_slots");
                byte cashSlots = query.Get<byte>("cash_slots");

                Items = new PlayerItems(this, itemQuery, meso, equipmentSlots, usableSlots, setupSlots, etceteraSlots, cashSlots);
            }

            using (DatabaseQuery skillQuery = Database.Query("SELECT * FROM player_skill WHERE player_identifier=@player_identifier", new MySqlParameter("player_identifier", Identifier)))
            {
                Skills = new PlayerSkills(this, skillQuery);
            }

            using (DatabaseQuery questQuery = Database.Query("SELECT * FROM player_quest WHERE player_identifier=@player_identifier", new MySqlParameter("player_identifier", Identifier)))
            {
                Quests = new PlayerQuests(this, questQuery);
            }

            using (DatabaseQuery spTableQuery = Database.Query("SELECT * FROM player_sp_table WHERE player_identifier=@player_identifier", new MySqlParameter("player_identifier", Identifier)))
            {
                SPTable = new PlayerSPTable(this, spTableQuery);
            }

            ControlledMobs = new ControlledMobs(this);
            ControlledNpcs = new ControlledNpcs(this);
        }

        public void Save()
        {
            Map currentMap = MasterServer.Instance.GetMaps(Client.ChannelIdentifier)[MapIdentifier];

            currentMap.Players.Remove(this);

            SpawnPoint = currentMap.Portals.GetNearestSpawnPoint(Position).Identifier;

            Database.Execute("UPDATE player SET gender=@gender, skin=@skin, face=@face, hair=@hair, level=@level, job=@job, sub_job=@sub_job, strength=@strength, dexterity=@dexterity, intelligence=@intelligence, luck=@luck, health=@health, max_health=@max_health, mana=@mana, max_mana=@max_mana, ability_points=@ability_points, skill_points=@skill_points, experience=@experience, fame=@fame, map_identifier=@map_identifier, map_spawn=@map_spawn, meso=@meso, equipment_slots=@equipment_slots, usable_slots=@usable_slots, etcetera_slots=@etcetera_slots, cash_slots=@cash_slots WHERE identifier=@identifier",
                               new MySqlParameter("identifier", Identifier),
                               new MySqlParameter("gender", Gender),
                               new MySqlParameter("skin", Skin),
                               new MySqlParameter("face", Face),
                               new MySqlParameter("hair", Hair),
                               new MySqlParameter("level", Stats.Level),
                               new MySqlParameter("job", Stats.Job),
                               new MySqlParameter("sub_job", Stats.SubJob),
                               new MySqlParameter("strength", Stats.Strength),
                               new MySqlParameter("dexterity", Stats.Dexterity),
                               new MySqlParameter("intelligence", Stats.Intelligence),
                               new MySqlParameter("luck", Stats.Luck),
                               new MySqlParameter("health", Stats.Health),
                               new MySqlParameter("max_health", Stats.MaxHealth),
                               new MySqlParameter("mana", Stats.Mana),
                               new MySqlParameter("max_mana", Stats.MaxMana),
                               new MySqlParameter("ability_points", Stats.AbilityPoints),
                               new MySqlParameter("skill_points", Stats.SkillPoints),
                               new MySqlParameter("experience", Stats.Experience),
                               new MySqlParameter("fame", Stats.Fame),
                               new MySqlParameter("map_identifier", MapIdentifier),
                               new MySqlParameter("map_spawn", SpawnPoint),
                               new MySqlParameter("meso", Items.Meso),
                               new MySqlParameter("equipment_slots", Items.EquipmentSlots),
                               new MySqlParameter("usable_slots", Items.UsableSlots),
                               new MySqlParameter("setup_slots", Items.SetupSlots),
                               new MySqlParameter("etcetera_slots", Items.EtceteraSlots),
                               new MySqlParameter("cash_slots", Items.CashSlots));

            Items.Save();
            Skills.Save();
            Quests.Save();
            SPTable.Save();
        }

        public void Initialize()
        {
            Send(PlayerPackets.EventNameTag(new sbyte[5] { -1, -1, -1, -1, -1 }));

            Send(MapPackets.ChangeMap(this, true));

            Notify("Welcome to SharpEnd!", EMessageType.Header);

            Map.Players.Add(this);

            IsInitialized = true;
        }

        public void Send(byte[] buffer)
        {
            Client.Send(buffer);
        }

        public void Release()
        {
            Client.Send(PlayerPackets.PlayerUpdate(this, EPlayerUpdate.None, true));
        }

        public void Notify(string text, EMessageType type = EMessageType.Pink)
        {
            Client.Send(MessagePackets.Notification(text, type));
        }

        public void SetMap(int mapIdentifier, sbyte portalIdentifier, Point position)
        {
            InternalSetMap(mapIdentifier, portalIdentifier, true, position);
        }

        public void SetMap(int mapIdentifier, PortalData portal = null, bool isInstance = false)
        {
            if (!MasterServer.Instance.GetMaps(Client.ChannelIdentifier).Contains(mapIdentifier))
            {
                return;
            }

            Map newMap = MasterServer.Instance.GetMaps(Client.ChannelIdentifier)[mapIdentifier];

            if (portal == null)
            {
                portal = newMap.Portals.GetSpawnPoint();
            }

            if (!isInstance)
            {

            }

            InternalSetMap(mapIdentifier, portal.Identifier, false, new Point(portal.Position.X, (short)(portal.Position.Y - 40)));
        }

        private void InternalSetMap(int mapIdentifier, sbyte portalIdentifier, bool spawnByPosition, Point position)
        {
            Map newMap = MasterServer.Instance.GetMaps(Client.ChannelIdentifier)[mapIdentifier];

            Map.Players.Remove(this);

            MapIdentifier = mapIdentifier;
            SpawnPoint = portalIdentifier;

            Position = position;
            Stance = 0;
            Foothold = 0;

            Send(MapPackets.ChangeMap(this, spawnByPosition: spawnByPosition, position: Position));

            newMap.Players.Add(this);
        }

        public void AcceptDeath(bool wheel)
        {
            int returnMapIdentifier = 0; // TODO: Change to map's return map.

            if (wheel)
            {
                returnMapIdentifier = MapIdentifier;
            }

            Stats.SetHealth(50, false);

            SetMap(returnMapIdentifier);
        }
    }
}
