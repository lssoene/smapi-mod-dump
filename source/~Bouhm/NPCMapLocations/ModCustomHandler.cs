using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NPCMapLocations
{
	// Handles custom maps (recolors of the mod map), custom NPCs, custom sprites, custom names, etc.
	internal class ModCustomHandler
	{
		private readonly IModHelper Helper;
		private readonly IMonitor Monitor;
		private ModConfig Config;
		private Dictionary<string, string> CustomNames; // For handling custom names
		private Dictionary<string, int> MarkerCropOffsets;
		private HashSet<string> NpcCustomizations;

		public ModCustomHandler(IModHelper helper, IMonitor monitor)
		{
			this.MarkerCropOffsets = ModConstants.MarkerCropOffsets;
			this.Helper = helper;
			this.Monitor = monitor;
			this.CustomNames = new Dictionary<string, string>();
			this.NpcCustomizations = new HashSet<string>();
		}

    public void LoadConfig(ModConfig config)
	  {
	    this.Config = config;
	  }

		// Handles customizations for NPCs
		// Custom NPCs and custom names or sprites for existing NPCs
		public void UpdateCustomNpcs()
		{
			foreach (NPC npc in Utility.getAllCharacters())
			{
				if (npc == null) continue;

				if (!ModConstants.ExcludedVillagers.Contains(npc.Name) && npc.isVillager())
				{
					LoadNpcCrop(npc);
					LoadCustomNames(npc);
				}
			}

			if (this.NpcCustomizations.Count != 0)
			{
				string names = "Handled custom NPCs: ";
				foreach (string name in this.NpcCustomizations)
				{
					names += name + ", ";
				}

				this.Monitor.Log(names.Substring(0, names.Length - 2), LogLevel.Info);
			}
			this.Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
		}

		// Load recolored map if user has recolor mods
		public string LoadMap()
		{
			// Eemie's Map Recolour
			if (this.Helper.ModRegistry.IsLoaded("minervamaga.CP.eemieMapRecolour"))
			{
				return "eemie_recolour_map";
			}
			// Starblue Valley
			else if (this.Helper.ModRegistry.IsLoaded("Lita.StarblueValley"))
			{
				return "starblue_map";
			}
			// Default
			else
			{
				return "default_map";
			}
		}

		public Dictionary<string, string> GetNpcNames()
		{
			return this.CustomNames;
		}

		public Dictionary<string, int> GetMarkerCropOffsets()
		{
			return this.MarkerCropOffsets;
		}

	  // { "customLocation": [{x, y}] } where x, y are relative to map (for a custom location ex. a new house)
    // { "customRegion": [{x1, y1}, {x2, y2}] where x, y are relative to map (for a custom region ex. a new farm)
	  // Any custom locations with given location on the map
	  public Dictionary<string, MapVector[]> GetCustomMapLocations()
	  {
      var customMapVectors = new Dictionary<string, MapVector[]>();
      foreach (KeyValuePair<string, JObject[]> mapVectors in Config.CustomMapLocations)
	    {
        var mapVectorArr = new MapVector[mapVectors.Value.Length];
	      for (int i = 0; i < mapVectors.Value.Length; i++)
	      {
          var mapVector = mapVectors.Value[i];

          // Marker doesn't need to specify corresponding Tile position
	        if (mapVector.GetValue("TileX") == null || mapVector.GetValue("TileY") == null)
	        {
	          mapVectorArr[i] = new MapVector(
	            (int) mapVector.GetValue("MapX"),
	            (int) mapVector.GetValue("MapY")
	          );
	        }
          // Region must specify corresponding Tile positions for
          // Calculations on movement within location
	        else
          {
            mapVectorArr[i] = new MapVector(
              (int)mapVector.GetValue("MapX"),
              (int)mapVector.GetValue("MapY"),
              (int)mapVector.GetValue("TileX"),
              (int)mapVector.GetValue("TileY")
            );
          }
	      }
	      customMapVectors.Add(mapVectors.Key, mapVectorArr);        
	    }

	    return customMapVectors;
	  }

    // Handle any modified NPC names 
    // Specifically mods that change names in dialogue files (displayName)
    private void LoadCustomNames(NPC npc)
		{
			if (!this.CustomNames.TryGetValue(npc.Name, out string customName))
			{
				if (npc.displayName == null)
					this.CustomNames.Add(npc.Name, npc.Name);
				else
				{
					this.CustomNames.Add(npc.Name, npc.displayName);
					if (!npc.Name.Equals(npc.displayName) || this.Config.CustomCropOffsets.ContainsKey(npc.Name))
						this.NpcCustomizations.Add(npc.Name);
				}
			}
		}

		// Load user-specified NPC crops for custom sprites
		private void LoadNpcCrop(NPC npc)
		{
			if (this.Config.CustomCropOffsets != null && this.Config.CustomCropOffsets.Count > 0)
			{
				foreach (KeyValuePair<string, int> villager in this.Config.CustomCropOffsets)
				{
					if (npc.Name.Equals(villager.Key))
					{
						this.MarkerCropOffsets[npc.Name] = villager.Value;
						this.NpcCustomizations.Add(npc.Name);
					}
				}
			}

			// If custom crop offset is not specified, default to 0
			if (!this.MarkerCropOffsets.TryGetValue(npc.Name, out int crop))
			{
				this.MarkerCropOffsets[npc.Name] = 0;
      }
		}
	}
}