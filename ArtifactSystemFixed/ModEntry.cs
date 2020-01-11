using StardewModdingAPI;
using Harmony;

#if DEBUG
using Microsoft.Xna.Framework;
using StardewValley;
#endif

namespace ArtifactSystemFixed
{
    internal class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            LoadConfig();

            var harmony = HarmonyInstance.Create("me.ilyaki.ArtifactSystemFixed");
            Patch.PatchAll(harmony);
#if DEBUG
    		//Debugging commands
			helper.ConsoleCommands.Add("asf_addspot", "Add an artifact dig spot where you are standing", this.AddArtifactDigSpot);
			helper.ConsoleCommands.Add("asf_addspots", "Add lots of dig spots (Do not use this!)", this.AddArtifactDigSpots);
#endif
            helper.ConsoleCommands.Add("asf_reloadconfig", "Reloads the config", (cmd, args) =>
            {
                LoadConfig();

                Monitor.Log("Reloaded config", LogLevel.Info);
            });
        }

        private void LoadConfig()
        {
            var config = Helper.ReadConfig<ModConfig>();
            GameLocation_digUpArtifactSpot_Patcher.Config = config;
            Utility_getTreasureFromGeode_Patcher.Config = config;
        }
#if DEBUG
		private void AddArtifactDigSpot(string command, string[] args)
		{
			var loc = Game1.player.getTileLocation();
			var obj = new StardewValley.Object(loc, 590, 1);
			Game1.currentLocation.objects.Add(loc, obj);
		}

		private void AddArtifactDigSpots(string command, string[] args)
		{
			var width = Game1.currentLocation.Map.Layers[0].LayerWidth;
			var height = Game1.currentLocation.Map.Layers[0].LayerHeight;

			for (var i = 0; i < width; i++)
			{
				for (var j = 0; j < height; j++)
				{
					var loc = new Vector2(i,j);
					if (!Game1.currentLocation.isTileOccupiedForPlacement(loc))
					{
						var obj = new StardewValley.Object(loc, 590, 1);
						Game1.currentLocation.objects.Add(loc, obj);
					}
				}
			}
		}
#endif
    }
}