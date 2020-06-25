﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedDoom
{
    public sealed class Map
    {
        private TextureLookup textures;
        private FlatLookup flats;

        private World world;

        private Vertex[] vertices;
        private Sector[] sectors;
        private SideDef[] sides;
        private LineDef[] lines;
        private Seg[] segs;
        private Subsector[] subsectors;
        private Node[] nodes;
        private MapThing[] things;
        private BlockMap blockMap;
        private Reject reject;

        private Texture skyTexture;

        private string title;

        public Map(CommonResource resorces, World world)
            : this(resorces.Wad, resorces.Textures, resorces.Flats, world)
        {
        }

        public Map(Wad wad, TextureLookup textures, FlatLookup flats, World world)
        {
            this.textures = textures;
            this.flats = flats;
            this.world = world;

            var options = world.Options;

            string name;
            if (wad.Names.Contains("doom") || wad.Names.Contains("doom1"))
            {
                name = "E" + options.Episode + "M" + options.Map;
            }
            else
            {
                name = "MAP" + options.Map.ToString("00");
            }

            var map = wad.GetLumpNumber(name);
            vertices = Vertex.FromWad(wad, map + 4);
            sectors = Sector.FromWad(wad, map + 8, flats);
            sides = SideDef.FromWad(wad, map + 3, textures, sectors);
            lines = LineDef.FromWad(wad, map + 2, vertices, sides);
            segs = Seg.FromWad(wad, map + 5, vertices, lines);
            subsectors = Subsector.FromWad(wad, map + 6, segs);
            nodes = Node.FromWad(wad, map + 7, subsectors);
            things = MapThing.FromWad(wad, map + 1);
            blockMap = BlockMap.FromWad(wad, map + 10, lines);
            reject = Reject.FromWad(wad, map + 9, sectors);

            GroupLines();

            skyTexture = GetSkyTextureByMapName(name);

            if (options.GameMode == GameMode.Commercial)
            {
                switch (options.MissionPack)
                {
                    case MissionPack.Plutonia:
                        title = DoomInfo.MapTitles.Plutonia[options.Map - 1];
                        break;
                    case MissionPack.Tnt:
                        title = DoomInfo.MapTitles.Tnt[options.Map - 1];
                        break;
                    default:
                        title = DoomInfo.MapTitles.Doom2[options.Map - 1];
                        break;
                }
            }
            else
            {
                title = DoomInfo.MapTitles.Doom[options.Episode - 1][options.Map - 1];
            }
        }

        private void GroupLines()
        {
            var sectorLines = new List<LineDef>();
            var bbox = new Fixed[4];

            foreach (var line in lines)
            {
                if (line.Special != 0)
                {
                    var mo = ThinkerPool.RentMobj(world);
                    mo.X = (line.Vertex1.X + line.Vertex2.X) / 2;
                    mo.Y = (line.Vertex1.Y + line.Vertex2.Y) / 2;
                    line.SoundOrigin = mo;
                }
            }

            foreach (var sector in sectors)
            {
                sectorLines.Clear();
                Box.Clear(bbox);

                foreach (var line in lines)
                {
                    if (line.FrontSector == sector || line.BackSector == sector)
                    {
                        sectorLines.Add(line);
                        Box.AddPoint(bbox, line.Vertex1.X, line.Vertex1.Y);
                        Box.AddPoint(bbox, line.Vertex2.X, line.Vertex2.Y);
                    }
                }

                sector.Lines = sectorLines.ToArray();

                // set the degenmobj_t to the middle of the bounding box
                sector.SoundOrigin = ThinkerPool.RentMobj(world);
                sector.SoundOrigin.X = (bbox[Box.Right] + bbox[Box.Left]) / 2;
                sector.SoundOrigin.Y = (bbox[Box.Top] + bbox[Box.Bottom]) / 2;

                sector.BlockBox = new int[4];
                int block;

                // adjust bounding box to map blocks
                block = (bbox[Box.Top] - blockMap.OriginY + GameConstants.MaxThingRadius).Data >> BlockMap.MapBlockShift;
                block = block >= blockMap.Height ? blockMap.Height - 1 : block;
                sector.BlockBox[Box.Top] = block;

                block = (bbox[Box.Bottom] - blockMap.OriginY - GameConstants.MaxThingRadius).Data >> BlockMap.MapBlockShift;
                block = block < 0 ? 0 : block;
                sector.BlockBox[Box.Bottom] = block;

                block = (bbox[Box.Right] - blockMap.OriginX + GameConstants.MaxThingRadius).Data >> BlockMap.MapBlockShift;
                block = block >= blockMap.Width ? blockMap.Width - 1 : block;
                sector.BlockBox[Box.Right] = block;

                block = (bbox[Box.Left] - blockMap.OriginX - GameConstants.MaxThingRadius).Data >> BlockMap.MapBlockShift;
                block = block < 0 ? 0 : block;
                sector.BlockBox[Box.Left] = block;
            }
        }

        private Texture GetSkyTextureByMapName(string name)
        {
            if (name.Length == 4)
            {
                return textures["SKY1"];
            }
            else
            {
                var number = int.Parse(name.Substring(3));
                if (number <= 11)
                {
                    return textures["SKY1"];
                }
                else if (number <= 21)
                {
                    return textures["SKY2"];
                }
                else
                {
                    return textures["SKY3"];
                }
            }
        }

        public TextureLookup Textures => textures;
        public FlatLookup Flats => flats;
        public Vertex[] Vertices => vertices;
        public Sector[] Sectors => sectors;
        public SideDef[] Sides => sides;
        public LineDef[] Lines => lines;
        public Seg[] Segs => segs;
        public Subsector[] Subsectors => subsectors;
        public Node[] Nodes => nodes;
        public MapThing[] Things => things;
        public BlockMap BlockMap => blockMap;
        public Reject Reject => reject;
        public Texture SkyTexture => skyTexture;
        public int SkyFlatNumber => flats.SkyFlatNumber;
        public string Title => title;
    }
}
