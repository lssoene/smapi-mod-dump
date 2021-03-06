﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace FarmTypeManager
{
    /// <summary>Methods used repeatedly by other sections of this mod, e.g. to locate tiles.</summary>
    static class Utility
    {
        /// <summary>Produces a list of x/y coordinates for valid, open tiles for object spawning at a location (based on tile index, e.g. tiles using a specific dirt texture).</summary>
        /// <param name="locationName">The name of the GameLocation to check.</param>
        /// <param name="tileIndices">A list of integers representing spritesheet tile indices. Tiles with any matching index will be checked for object spawning.</param>
        /// <returns>A list of Vector2, each representing a valid, open tile for object spawning at the given location.</returns>
        public static List<Vector2> GetTilesByIndex(string locationName, int[] tileIndices)
        {
            GameLocation loc = Game1.getLocationFromName(locationName); //variable for the current location being worked on
            List<Vector2> validTiles = new List<Vector2>(); //will contain x,y coordinates for tiles that are open & valid for new object placement

            //the following loops should populate a list of valid, open tiles for spawning
            int currentTile;
            for (int y = 0; y < (loc.Map.DisplayHeight / Game1.tileSize); y++)
            {
                for (int x = 0; x < (loc.Map.DisplayWidth / Game1.tileSize); x++) //loops for each tile on the map, from the top left (x,y == 0,0) to bottom right, moving horizontally first
                {
                    currentTile = loc.getTileIndexAt(x, y, "Back"); //get the tile index of the current tile
                    foreach (int index in tileIndices)
                    {
                        if (currentTile == index) //if the current tile matches one of the tile indices
                        {
                            if (loc.isTileLocationTotallyClearAndPlaceable(x, y) == true) //if the tile is clear of any obstructions
                            {
                                validTiles.Add(new Vector2(x, y)); //add to list of valid spawn tiles
                            }
                        }
                    }
                }
            }
            return validTiles;
        }

        /// <summary>Produces a list of x/y coordinates for valid, open tiles for object spawning at a location (based on tile properties, e.g. the "grass" type).</summary>
        /// <param name="locationName">The name of the GameLocation to check.</param>
        /// <param name="type">A string representing the tile property to match, or a special term used for some additional checks.</param>
        /// <returns>A list of Vector2, each representing a valid, open tile for object spawning at the given location.</returns>
        public static List<Vector2> GetTilesByProperty(string locationName, string type)
        {
            GameLocation loc = Game1.getLocationFromName(locationName); //variable for the current location being worked on
            List<Vector2> validTiles = new List<Vector2>(); //will contain x,y coordinates for tiles that are open & valid for new object placement

            //the following loops should populate a list of valid, open tiles for spawning
            for (int y = 0; y < (loc.Map.DisplayHeight / Game1.tileSize); y++)
            {
                for (int x = 0; x < (loc.Map.DisplayWidth / Game1.tileSize); x++) //loops for each tile on the map, from the top left (x,y == 0,0) to bottom right, moving horizontally first
                {
                    if (type.Equals("all", StringComparison.OrdinalIgnoreCase)) //if the "property" to be matched is "All" (a special exception)
                    {
                        //add any clear tiles, regardless of properties
                        if (loc.isTileLocationTotallyClearAndPlaceable(x, y) == true) //if the tile is clear of any obstructions
                        {
                            validTiles.Add(new Vector2(x, y)); //add to list of valid spawn tiles
                        }
                    }
                    if (type.Equals("diggable", StringComparison.OrdinalIgnoreCase)) //if the tile's "Diggable" property matches (case-insensitive)
                    {
                        if (loc.doesTileHaveProperty(x, y, "Diggable", "Back") == "T") //NOTE: the string "T" means "true" for several tile property checks
                        {
                            if (loc.isTileLocationTotallyClearAndPlaceable(x, y) == true) //if the tile is clear of any obstructions
                            {
                                validTiles.Add(new Vector2(x, y)); //add to list of valid spawn tiles
                            }
                        }
                    }
                    else //assumed to be checking for a specific value in the tile's "Type" property, e.g. "Grass" or "Dirt"
                    {
                        string currentType = loc.doesTileHaveProperty(x, y, "Type", "Back") ?? ""; //NOTE: this sets itself to a blank (not null) string to avoid null errors when comparing it

                        if (currentType.Equals(type, StringComparison.OrdinalIgnoreCase)) //if the tile's "Type" property matches (case-insensitive)
                        {
                            if (loc.isTileLocationTotallyClearAndPlaceable(x, y) == true) //if the tile is clear of any obstructions
                            {
                                validTiles.Add(new Vector2(x, y)); //add to list of valid spawn tiles
                            }
                        }
                    }
                }
            }
            return validTiles;
        }

        /// <summary>Produces a list of x/y coordinates for valid, open tiles for object spawning at a location (based on a string describing two vectors).</summary>
        /// <param name="locationName">The name of the GameLocation to check.</param>
        /// <param name="vectorString">A string describing two vectors. Parsed into vectors and used to find a rectangular area.</param>
        /// <returns>A list of Vector2, each representing a valid, open tile for object spawning at the given location.</returns>
        public static List<Vector2> GetTilesByVectorString(string locationName, string vectorString)
        {
            GameLocation loc = Game1.getLocationFromName(locationName); //variable for the current location being worked on
            List<Vector2> validTiles = new List<Vector2>(); //x,y coordinates for tiles that are open & valid for new object placement
            List<Tuple<Vector2, Vector2>> vectorPairs = new List<Tuple<Vector2, Vector2>>(); //pairs of x,y coordinates representing areas on the map (to be scanned for valid tiles)

            //parse the "raw" string representing two coordinates into actual numbers, populating "vectorPairs"
            string[] xyxy = vectorString.Split(new char[] { ',', '/', ';' }); //split the string into separate strings based on various delimiter symbols
            if (xyxy.Length != 4) //if "xyxy" didn't split into the right number of strings, it's probably formatted poorly
            {
                Monitor.Log($"Issue: This include/exclude area for the {locationName} map isn't formatted correctly: \"{vectorString}\"", LogLevel.Info);
            }
            else
            {
                int[] numbers = new int[4]; //this section will convert "xyxy" into four numbers and store them here
                bool success = true;
                for (int i = 0; i < 4; i++)
                {
                    if (Int32.TryParse(xyxy[i].Trim(), out numbers[i]) != true) //attempts to store each "xyxy" string as an integer in "numbers"; returns false if it failed
                    {
                        success = false;
                    }
                }

                if (success) //everything was successfully parsed, apparently
                {
                    //convert the numbers to a pair of vectors and add them to the list
                    vectorPairs.Add(new Tuple<Vector2, Vector2>(new Vector2(numbers[0], numbers[1]), new Vector2(numbers[2], numbers[3])));
                }
                else
                {
                    Monitor.Log($"Issue: This include/exclude area for the {locationName} map isn't formatted correctly: \"{vectorString}\"", LogLevel.Info);
                }
            }

            //check the area marked by "vectorPairs" for valid, open tiles and populate "validTiles" with them
            foreach (Tuple<Vector2, Vector2> area in vectorPairs)
            {
                for (int y = (int)Math.Min(area.Item1.Y, area.Item2.Y); y <= (int)Math.Max(area.Item1.Y, area.Item2.Y); y++) //use the lower Y first, then the higher Y; should define the area regardless of which corners/order the user wrote down
                {
                    for (int x = (int)Math.Min(area.Item1.X, area.Item2.X); x <= (int)Math.Max(area.Item1.X, area.Item2.X); x++) //loops for each tile on the map, from the top left (x,y == 0,0) to bottom right, moving horizontally first
                    {
                        if (loc.isTileLocationTotallyClearAndPlaceable(x, y) == true) //if the tile is clear of any obstructions
                        {
                            validTiles.Add(new Vector2(x, y)); //add to list of valid spawn tiles
                        }
                    }
                }
            }

            return validTiles;
        }

        /// <summary>Generates a list of all valid tiles for object spawning in the provided SpawnArea.</summary>
        /// <param name="area">A SpawnArea listing an in-game map name and the valid regions/terrain within it that may be valid spawn points.</param>
        /// <param name="customTileIndex">The list of custom tile indices for this spawn process (e.g. forage or ore generation). Found in the relevant section of Utility.Config.</param>
        /// <param name="isLarge">True if the objects to be spawned are 2x2 tiles in size, otherwise false (1 tile).</param>
        /// <returns>A completed list of all valid tile coordinates for this spawn process in this SpawnArea.</returns>
        public static List<Vector2> GenerateTileList(SpawnArea area, int[] customTileIndex, bool isLarge)
        {
            List<Vector2> validTiles = new List<Vector2>(); //list of all open, valid tiles for new spawns on the current map

            foreach (string type in area.AutoSpawnTerrainTypes) //loop to auto-detect valid tiles based on various types of terrain
            {
                if (type.Equals("quarry", StringComparison.OrdinalIgnoreCase)) //add tiles matching the "quarry" tile index list
                {
                    validTiles.AddRange(Utility.GetTilesByIndex(area.MapName, Utility.Config.QuarryTileIndex));
                }
                else if (type.Equals("custom", StringComparison.OrdinalIgnoreCase)) //add tiles matching the "custom" tile index list
                {
                    validTiles.AddRange(Utility.GetTilesByIndex(area.MapName, customTileIndex));
                }
                else  //add any tiles with properties matching "type" (e.g. tiles with the "Diggable" property, "Grass" type, etc; if the "type" is "All", this will just add every valid tile)
                {
                    validTiles.AddRange(Utility.GetTilesByProperty(area.MapName, type));
                }
            }
            foreach (string include in area.IncludeAreas) //check for valid tiles in each "include" zone for the area
            {
                validTiles.AddRange(Utility.GetTilesByVectorString(area.MapName, include));
            }

            validTiles = validTiles.Distinct().ToList(); //remove any duplicate tiles from the list

            foreach (string exclude in area.ExcludeAreas) //check for valid tiles in each "exclude" zone for the area (validity isn't technically relevant here, but simpler to code, and tiles' validity cannot currently change during this process)
            {
                List<Vector2> excludedTiles = Utility.GetTilesByVectorString(area.MapName, exclude); //get list of valid tiles in the excluded area
                validTiles.RemoveAll(excludedTiles.Contains); //remove any previously valid tiles that match the excluded area
            }

            if (isLarge) //if working with 2x2 sized objects, e.g. stumps and logs
            {
                int x = 0;
                while (x < validTiles.Count) //loop until every remaining tile is valid for large objects
                {
                    if (!IsValidLargeSpawnLocation(area.MapName, validTiles[x])) //if the current tile is invalid for large objects
                    {
                        validTiles.Remove(validTiles[x]); //remove the tile from the list (NOTE: this affects the list's index and item count, so don't increment x here)
                    }
                    else //if the tile is valid
                    {
                        x++; //move on to the next tile in the list
                    }
                }
            }

            return validTiles;
        }

        /// <summary>Generates ore and places it on the specified map and tile.</summary>
        /// <param name="oreName">A string representing the name of the ore type to be spawned, e.g. "</param>
        /// <param name="mapName">The name of the GameLocation where the ore should be spawned.</param>
        /// <param name="tile">The x/y coordinates of the tile where the ore should be spawned.</param>
        public static void SpawnOre(string oreName, string mapName, Vector2 tile)
        {
            Random rng = new Random();
            StardewValley.Object ore = null; //ore object, to be spawned into the world later
            switch (oreName.ToLower()) //avoid any casing issues in method calls by making this lower-case
            {
                case "stone":
                    ore = new StardewValley.Object(tile, 668 + (rng.Next(2) * 2), 1); //either of the two random stones spawned in the vanilla hilltop quarry
                    ore.MinutesUntilReady = 2; //durability, i.e. number of hits with basic pickaxe required to break the ore (each pickaxe level being +1 damage)
                    break;
                case "geode":
                    ore = new StardewValley.Object(tile, 75, 1); //"regular" geode rock, as spawned on vanilla hilltop quarries 
                    ore.MinutesUntilReady = 3;
                    break;
                case "frozengeode":
                    ore = new StardewValley.Object(tile, 76, 1); //frozen geode rock
                    ore.MinutesUntilReady = 5;
                    break;
                case "magmageode":
                    ore = new StardewValley.Object(tile, 77, 1); //magma geode rock
                    ore.MinutesUntilReady = 8; //TODO: replace this guess w/ actual vanilla durability
                    break;
                case "gem":
                    ore = new StardewValley.Object(tile, (rng.Next(7) + 1) * 2, "Stone", true, false, false, false); //any of the possible gem rocks
                    ore.MinutesUntilReady = 5; //based on "gemstone" durability, but applies to every type for simplicity's sake
                    break;
                case "copper":
                    ore = new StardewValley.Object(tile, 751, 1); //copper ore
                    ore.MinutesUntilReady = 3;
                    break;
                case "iron":
                    ore = new StardewValley.Object(tile, 290, 1); //iron ore
                    ore.MinutesUntilReady = 4;
                    break;
                case "gold":
                    ore = new StardewValley.Object(tile, 764, 1); //gold ore
                    ore.MinutesUntilReady = 8;
                    break;
                case "iridium":
                    ore = new StardewValley.Object(tile, 765, 1); //iridium ore
                    ore.MinutesUntilReady = 16; //TODO: confirm this is still the case (it's based on SDV 1.11 code)
                    break;
                case "mystic":
                    ore = new StardewValley.Object(tile, 46, "Stone", true, false, false, false); //mystic ore, i.e. high-end cavern rock with iridium + gold
                    ore.MinutesUntilReady = 16; //TODO: replace this guess w/ actual vanilla durability
                    break;
                default: break;
            }

            if (ore != null)
            {
                GameLocation loc = Game1.getLocationFromName(mapName);
                loc.setObject(tile, ore); //actually spawn the ore object into the world
            }
            else
            {
                Utility.Monitor.Log($"The ore to be spawned (\"{oreName}\") doesn't match any known ore types. Make sure that name isn't misspelled in your player config file.", LogLevel.Info);
            }

            return;
        }

        /// <summary>Produces a dictionary containing the final, adjusted spawn chance of each object in the provided dictionaries. (Part of the convoluted object spawning process for ore.)</summary>
        /// <param name="skill">The player skill that affects spawn chances (e.g. Mining for ore spawn chances).</param>
        /// <param name="levelRequired">A dictionary of object names and the skill level required to spawn them.</param>
        /// <param name="startChances">A dictionary of object names and their weighted chances to spawn at their lowest required skill level (e.g. chance of spawning stone if you're level 0).</param>
        /// <param name="maxChances">A dictionary of object names and their weighted chances to spawn at skill level 10.</param>
        /// <returns></returns>
        public static Dictionary<string, int> AdjustedSpawnChances(Utility.Skills skill, Dictionary<string, int> levelRequired, Dictionary<string, int> startChances, Dictionary<string, int> maxChances)
        {
            Dictionary<string, int> adjustedChances = new Dictionary<string, int>();

            int skillLevel = 0; //highest skill level among all existing farmers, not just the host
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                skillLevel = Math.Max(skillLevel, farmer.getEffectiveSkillLevel((int)skill)); //record the new level if it's higher than before
            }

            foreach (KeyValuePair<string, int> objType in levelRequired)
            {
                int chance = 0; //chance of spawning this object
                if (objType.Value > skillLevel)
                {
                    //skill is too low to spawn this object; leave it at 0%
                }
                else if (objType.Value == skillLevel)
                {
                    chance = startChances[objType.Key]; //skill is the minimum required; use the starting chance
                }
                else if (skillLevel >= 10)
                {
                    chance = maxChances[objType.Key]; //level 10 skill; use the max level chance
                }
                else //skill is somewhere in between "starting" and "level 10", so do math to set the chance somewhere in between them (i forgot the term for this kind of averaging, sry)
                {
                    int count = 0;
                    long chanceMath = 0; //used in case the chances are very large numbers for some reason
                    for (int x = objType.Value; x < 10; x++) //loop from [minimum skill level for this object] to [max level - 1], for vague math reasons
                    {
                        if (skillLevel > x)
                        {
                            chanceMath += maxChances[objType.Key]; //add level 10 chance
                        }
                        else
                        {
                            chanceMath += startChances[objType.Key]; //add starting chance
                        }
                        count++;
                    }
                    chanceMath = (long)Math.Round((double)chanceMath / (double)count); //divide to get the average
                    chance = Convert.ToInt32(chanceMath); //convert back to a reasonable number range once the math is done
                }

                if (chance > 0) //don't bother adding any objects with 0% or negative spawn chance
                {
                    adjustedChances.Add(objType.Key, chance); //add the object name & chance to the list of adjusted chances
                }
            }

            return adjustedChances;
        }

        /// <summary>Calculates the final number of objects to spawn today in the current spawning process, based on config settings and player levels in a relevant skill.</summary>
        /// <param name="min">Minimum number of objects to spawn today (before skill multiplier).</param>
        /// <param name="max">Maximum number of objects to spawn today (before skill multiplier).</param>
        /// <param name="percent">Additive multiplier for each of the player's levels in the relevant skill (e.g. 10 would represent +10% objects per level).</param>
        /// <param name="skill">Enumerator for the skill on which the "percent" additive multiplier is based.</param>
        /// <returns>The final number of objects to spawn today in the current spawning process.</returns>
        public static int AdjustedSpawnCount(int min, int max, int percent, Utility.Skills skill)
        {
            Random rng = new Random(); //DEVNOTE: "Game1.random" exists, but causes some odd spawn behavior; using this for now...
            int spawnCount = rng.Next(min, max + 1); //random number from min to max (higher number is exclusive, so +1 to adjust for it)

            //calculate skill multiplier bonus
            double skillMultiplier = percent;
            skillMultiplier = (skillMultiplier / 100); //converted to percent, e.g. default config is "10" (10% per level) so it converts to "0.1"
            int highestSkillLevel = 0; //highest skill level among all existing farmers, not just the host
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                highestSkillLevel = Math.Max(highestSkillLevel, farmer.getEffectiveSkillLevel((int)skill)); //record the new level if it's higher than before
            }
            skillMultiplier = 1.0 + (skillMultiplier * highestSkillLevel); //final multiplier; e.g. with default config: "1.0" at level 0, "1.7" at level 7, etc

            //calculate final forage amount
            skillMultiplier *= spawnCount; //multiply the initial random spawn count by the skill multiplier
            spawnCount = (int)skillMultiplier; //store the integer portion of the current multiplied value (e.g. this is "1" if the multiplier is "1.7")
            double remainder = skillMultiplier - (int)skillMultiplier; //store the decimal portion of the multiplied value (e.g. this is "0.7" if the multiplier is "1.7")

            if (rng.NextDouble() < remainder) //use remainder as a % chance to spawn one extra object (e.g. if the final count would be "1.7", there's a 70% chance of spawning 2 objects)
            {
                spawnCount++;
            }

            return spawnCount;
        }

        /// <summary>Determines whether a given tile is a valid spawn location for large objects (2x2 size).</summary>
        /// <param name="mapName">The map on which the object should be spawned.</param>
        /// <param name="tile">The tile at which the object is located (which is effectively its upper left corner).</param>
        /// <returns>A boolean indicating whether the tile is a valid spawn location for large objects.</returns>
        public static bool IsValidLargeSpawnLocation(string mapName, Vector2 tile)
        {
            bool valid = false;

            GameLocation loc = Game1.getLocationFromName(mapName); //variable for the current location being worked on 

            //if any of the necessary tiles for a 2x2 object are invalid, remove this tile from the list (note: the tile in the list is treated as the top left corner of the object)
            if (loc.isTileLocationTotallyClearAndPlaceable((int)tile.X, (int)tile.Y) && loc.isTileLocationTotallyClearAndPlaceable((int)tile.X + 1, (int)tile.Y) && loc.isTileLocationTotallyClearAndPlaceable((int)tile.X, (int)tile.Y + 1) && loc.isTileLocationTotallyClearAndPlaceable((int)tile.X + 1, (int)tile.Y + 1))
            {
                valid = true;
            }

            return valid;
        }

        /// <summary>Generate an array of index numbers for large objects (a.k.a. resource clumps) based on an array of names. Duplicates are allowed; invalid entries are discarded.</summary>
        /// <param name="names">A list of names representing large objects (e.g. "Stump", "boulders").</param>
        /// <returns>An array of index numbers for large object spawning purposes.</returns>
        public static List<int> GetLargeObjectIDs(string[] names)
        {
            List<int> IDs = new List<int>(); //a list of index numbers to be returned

            foreach (string name in names)
            {
                //for each valid name, add the game's internal ID for that large object (a.k.a. resource clump)
                switch (name.ToLower())
                {
                    case "stump":
                    case "stumps":
                        IDs.Add(600);
                        break;
                    case "log":
                    case "logs":
                        IDs.Add(602);
                        break;
                    case "boulder":
                    case "boulders":
                        IDs.Add(672);
                        break;
                    case "meteor":
                    case "meteors":
                    case "meteorite":
                    case "meteorites":
                        IDs.Add(622);
                        break;
                    case "minerock1":
                    case "mine rock 1":
                        IDs.Add(752);
                        break;
                    case "minerock2":
                    case "mine rock 2":
                        IDs.Add(754);
                        break;
                    case "minerock3":
                    case "mine rock 3":
                        IDs.Add(756);
                        break;
                    case "minerock4":
                    case "mine rock 4":
                        IDs.Add(758);
                        break;
                    default: //"name" isn't recognized as any existing object names
                        int parsed;
                        if (int.TryParse(name, out parsed)) //if the string seems to be a valid integer, save it to "parsed" and add it to the list
                        {
                            IDs.Add(parsed);
                        }
                        break;
                }
            }

            return IDs;
        }

        /// <summary>Encapsulates IMonitor.Log() for this mod's static classes. Must be given an IMonitor in the ModEntry class to produce output.</summary>
        public static class Monitor
        {
            private static IMonitor monitor;

            public static IMonitor IMonitor
            {
                set
                {
                    monitor = value;
                }
            }

            /// <summary>Log a message for the player or developer.</summary>
            /// <param name="message">The message to log.</param>
            /// <param name="level">The log severity level.</param>
            public static void Log(string message, LogLevel level = LogLevel.Debug)
            {
                if (monitor != null)
                {
                    monitor.Log(message, level);
                }
            }
        }

        /// <summary>Enumerated list of player skills, in the order used by Stardew's internal code (e.g. Farmer.cs).</summary>
        public enum Skills { Farming, Fishing, Foraging, Mining, Combat, Luck }

        /// <summary>Data contained in the per-character configuration file, including various mod settings.</summary>
        public static FarmConfig Config { get; set; } = null;

        /// <summary>Whether the mod has made any changes to the config settings for the current player (which requires the player's config file to the updated).</summary>
        public static bool HasConfigChanged { get; set; } = false;

    }
}
