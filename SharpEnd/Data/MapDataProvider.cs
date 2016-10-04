﻿using reNX;
using reNX.NXProperties;
using SharpEnd.Drawing;
using SharpEnd.Maps;
using System;
using System.Collections.Generic;
using System.IO;

namespace SharpEnd.Data
{
    internal sealed class MapDataProvider
    {
        private Dictionary<int, Map> m_maps;

        public MapDataProvider()
        {
            m_maps = new Dictionary<int, Map>();
        }

        private static List<string> skippedCategories = new List<string>()
        {
            "AdditionalInfo_JP.img", "AreaCode.img", "FieldGenerator.img", "Graph.img"
        };

        public void Load()
        {
            using (NXFile file = new NXFile(Path.Combine(Application.NXPath, "Map.nx")))
            {
                foreach (var category in file.BaseNode["Map"])
                {
                    if (skippedCategories.Contains(category.Name))
                    {
                        continue;
                    }

                    foreach (var node in category)
                    {
                        int identifier = node.GetIdentifier<int>();

                        Map map = new Map(identifier);

                        var infoNode = node["info"];

                        // TODO: Fetch info from infoNode

                        if (node.ContainsChild("life"))
                        {
                            LoadLife(map, node["life"]);
                        }

                        if (node.ContainsChild("portal"))
                        {
                            LoadPortals(map, node["portal"]);
                        }

                        m_maps.Add(identifier, map);
                    }
                }
            }

            Console.WriteLine($"Loaded {m_maps.Count} maps.");
        }

        private void LoadLife(Map map, NXNode lifeNode)
        {
            // NOTE: Some maps have life in categories.
            // We're going to skip them for now until we figure this one out.
            if (lifeNode.ContainsChild("isCategory"))
            {
                return;
            }

            foreach (var node in lifeNode)
            {
                int identifier = int.Parse(node.GetString("id")); // NOTE: Life identifiers are string
                Point position = new Point(node.GetShort("x"), node.GetShort("cy"));
                ushort foothold = 0;
                bool flip = node.GetBoolean("f");
                bool hide = node.GetBoolean("hide");

                switch (node.GetString("type"))
                {
                    case "n":
                        {
                            short cy = node.GetShort("cy");
                            short rx0 = node.GetShort("rx0");
                            short rx1 = node.GetShort("rx1");

                            Npc npc = new Npc(identifier, rx0, rx1, position, foothold, flip, hide);

                            map.Npcs.Add(npc);
                        }
                        break;

                    case "m":
                        {

                        }
                        break;

                    case "r":
                        {

                        }
                        break;
                }
            }
        }

        // TODO: Use the "pt" property which indicates the type of the portal (spawn point, door, etcetera)
        private void LoadPortals(Map map, NXNode portalNode)
        {
            foreach (var node in portalNode)
            {
                try
                {
                    PortalData portal = new PortalData();

                    portal.Identifier = node.GetIdentifier<sbyte>();
                    portal.Label = node.GetString("pn");
                    portal.DestinationMap = node.GetInt("tm");
                    portal.DestinationLabel = node.GetString("tn");
                    portal.Position = new Point(node.GetShort("x"), node.GetShort("y"));

                    if (portal.Label == "sp")
                    {
                        map.Portals.SpawnPoints.Add(portal);
                    }
                    else
                    {
                        map.Portals.Regular.Add(portal);
                    }
                }
                catch
                {
                    // NOTE: Some maps include way too many portals. We'll deal with them later.
                    // For now, we're skipping them.
                }
            }
        }

        public bool Contains(int identifier)
        {
            return m_maps.ContainsKey(identifier);
        }

        public Map this[int identifier]
        {
            get
            {
                return m_maps.GetOrDefault(identifier, null);
            }
        }
    }

    internal sealed class PortalData
    {
        public sbyte Identifier { get; set; }
        public string Label { get; set; }
        public int DestinationMap { get; set; }
        public string DestinationLabel { get; set; }
        public Point Position { get; set; }
    }
}