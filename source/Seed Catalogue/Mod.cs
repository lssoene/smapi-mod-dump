﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using SpaceCore.Events;

namespace SeedCatalogue
{
    public class Mod : StardewModdingAPI.Mod
    {
        public static Mod instance;

        public override void Entry(IModHelper helper)
        {
            instance = this;

            PlayerEvents.Warped += locChanged;
            SpaceEvents.ActionActivated += actionTriggered;

            Helper.ConsoleCommands.Add("seedcatalogue", "Open the seed catalogue. Do `seedcatalogue cheat` to show all seeds, even for crops you haven't shipped yet.", command);
        }

        private void locChanged( object sender, EventArgsPlayerWarped args )
        {
            if ( args.NewLocation is SeedShop ss )
            {
                ss.setTileProperty(1, 25, "Buildings", "Action", "SeedCatalogue");
            }
        }

        private void actionTriggered( object sender, EventArgsAction args )
        {
            if (args.Action == "SeedCatalogue")
                openSeedCatalogue();
        }

        private void command( string cmd, string[] args )
        {
            bool cheat = false;
            if (args.Length >= 1 && args[0] == "cheat")
                cheat = true;

            openSeedCatalogue(cheat);
        }

        private void openSeedCatalogue( bool cheat = false )
        {
            List<Item> items = new List<Item>();
            var cropData = Helper.Content.Load<Dictionary<int, string>>("Data\\Crops", ContentSource.GameContent);

            foreach ( var crop in cropData )
            {
                var values = crop.Value.Split('/');
                var product = Convert.ToInt32(values[3]);
                if (cheat || Game1.player.basicShipped.ContainsKey(product))
                {
                    var item = new StardewValley.Object(crop.Key, int.MaxValue);
                    // Strawberry seeds have a default price of 0, but in the festival shop are set to 100
                    if (!item.bigCraftable.Value && item.ParentSheetIndex == 745)
                        item.Price = 100 / 2; // Without /2 it comes out to 200
                    items.Add(item);
                }
            }

            Game1.activeClickableMenu = new ShopMenu(items);
        }
    }
}
