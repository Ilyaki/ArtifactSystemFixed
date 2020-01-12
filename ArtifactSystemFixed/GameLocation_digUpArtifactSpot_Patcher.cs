using System.Collections.Generic;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using System.Globalization;
using StardewValley;
using System.Linq;
using System;

using static ArtifactSystemFixed.Utils;

namespace ArtifactSystemFixed
{
    internal class GameLocation_digUpArtifactSpot_Patcher : Patch
    {
        public static ModConfig Config { private get; set; } = null;

        protected override PatchDescriptor GetPatchDescriptor()
        {
            return new PatchDescriptor(new GameLocation().GetType(), "digUpArtifactSpot");
        }

        //This is called on the client, so each player must install the mod if they want the fixes
        public static bool Prefix(GameLocation __instance, int xLocation, int yLocation, Farmer who)
        {
            var currentLocationName = __instance.Name;

            /////
            var random = new Random(xLocation * 2000 + yLocation + (int) Game1.uniqueIDForThisGame / 2 + (int) Game1.stats.DaysPlayed);

            #region Get a list of all the artifacts that can be found in our location

            var itemIDs = new Dictionary<int, double>();
            foreach (var item in Game1.objectInformation)
            {
                /* Should be like:
                    Ornamental Fan
                    300
                    -300
                    Arch
                    Ornamental Fan
                    This exquisite fan most likely belonged to a noblewoman. Historians believe that the valley was a popular sixth-era vacation spot for the wealthy.
                    Beach .02 Town .008 Forest .01
                    Money 1 300
                  */
                var itemInfo = item.Value.Split('/');

                if (itemInfo[3].Contains("Arch"))
                {
                    /* Should be in the format of Mountain .03 Forest .03 BusStop .04
                     * e.g. this would mean 3% in Mountain or Forest and 4% in BusStop
                     * the erroneous artifact data was fixed in Stardew Valley 1.4
                     */
                    var locationsAndItemsWeights = itemInfo[6].Split(' ');

                    for (var i = 0; i < locationsAndItemsWeights.Length; i += 2)
                    {
                        if (locationsAndItemsWeights[i] == currentLocationName)
                        {
                            var itemID = item.Key;
                            var weight = Convert.ToDouble(locationsAndItemsWeights[i + 1], CultureInfo.InvariantCulture);

                            itemIDs.Add(itemID, weight);
                        }
                    }
                }
            }

            #endregion

            #region Apply factor to weight of artifacts that have already been found

            foreach (var itemID in itemIDs.Keys.Reverse())
            {
                var weight = itemIDs[itemID];
                weight *= Math.Pow(Config.Artifact_AlreadyFoundMultiplier, GetNumberOfArtifactFound(itemID));
                itemIDs.Remove(itemID);
                itemIDs.Add(itemID, weight);
            }

            #endregion

            var initialTotalWeight = itemIDs.Values.Sum();
            var nothingWeight = Config.Artifact_BaseWeightForNothingInPrimaryTable 
                              + initialTotalWeight * Config.Artifact_MultiplierForNothingInPrimaryTable;
            itemIDs.Add(-1, nothingWeight);

            Console.WriteLine($"Artifact weight is {initialTotalWeight} out of total {initialTotalWeight + nothingWeight}");

            var toDigUpItemID = ChooseWeightedProbability(itemIDs, random, -1);
            
            Console.WriteLine($"Primary table gave itemID of {toDigUpItemID}");
            //Choose lost book (or mixed seeds if player has all the books)
            //102 is ID of a lost book, and 770 is ID of mixed seeds
            if (toDigUpItemID == -1 && random.NextDouble() < 0.2 && !(__instance is Farm)) 
                toDigUpItemID = 102;
            if (toDigUpItemID == 102 && who.archaeologyFound.ContainsKey(102) && who.archaeologyFound[102][0] >= 21)
                toDigUpItemID = 770;

            //Spawn the chosen item, or find a different item from winter items or Data\\Locations
            if (toDigUpItemID != -1)
            {
                Game1.createObjectDebris(toDigUpItemID, xLocation, yLocation, who.UniqueMultiplayerID);
                who.gainExperience(5, 25);
            }
            else if (Game1.currentSeason.Equals("winter") && random.NextDouble() < 0.5 && !(__instance is Desert))
            {
                Game1.createObjectDebris(random.NextDouble() < 0.4
                        ? 416 //Snow Yam
                        : 412, //Winter Root
                    xLocation, yLocation, who.UniqueMultiplayerID);
            }
            else
            {
                ChooseAndSpawnItemFromLocationsData(currentLocationName, random, __instance, xLocation, yLocation, who);
            }

            return false;
        }

        private static void ChooseAndSpawnItemFromLocationsData(string currentLocationName, Random random,
            GameLocation __instance, int xLocation, int yLocation, Farmer who)
        {
            /* This is the secondary place loot is defined : Game1.content.Load<Dictionary<string, string>>("Data\\Locations")
             * If something is not chosen from Game1.objectInformation, this is used.
             * Each location (e.g. Farm, BusStop) defines some IDs and the probabilities they are chosen
             * (This has not been modified from the vanilla method)
             */

            /////

            var locationData = Game1.content.Load<Dictionary<string, string>>("Data\\Locations");
            if (locationData.ContainsKey(currentLocationName))
            {
                var rawData = locationData[currentLocationName].Split('/')[8].Split(' '); //The loot is on line 8
                if (rawData.Length != 0 && !rawData[0].Equals("-1"))
                {
                    #region Choose an item from Data\\Locations

                    var i = 0;
                    while (true)
                    {
                        if (i < rawData.Length)
                        {
                            if (!(random.NextDouble() <= Convert.ToDouble(rawData[i + 1])))
                            {
                                i += 2;
                                continue;
                            }

                            break;
                        }

                        return;
                    }

                    #endregion

                    var toDigUpItemID = Convert.ToInt32(rawData[i]);

                    //Spawn the chosen item if it is an artifact. If not(e.g. clay), if the player found clay && magnifying glass && 11% chance then spawn secret note
                    if (Game1.objectInformation.ContainsKey(toDigUpItemID) && (Game1.objectInformation[toDigUpItemID].Split('/')[3].Contains("Arch") || toDigUpItemID == 102))
                    {
                        if (toDigUpItemID == 102 && who.archaeologyFound.ContainsKey(102) && who.archaeologyFound[102][0] >= 21)
                            toDigUpItemID = 770; //Choose mixed seeds if the player already has every lost book
                        Console.WriteLine($"Secondary table gave itemID of {toDigUpItemID}");
                        Game1.createObjectDebris(toDigUpItemID, xLocation, yLocation, who.UniqueMultiplayerID);
                    }
                    else
                    {
                        //The secret notes added in Stardew Valley 1.3
                        if (toDigUpItemID == 330 && who.hasMagnifyingGlass && Game1.random.NextDouble() < 0.11)
                        {
                            var secretNote = __instance.tryToCreateUnseenSecretNote(who);

                            if (secretNote != null)
                            {
                                Game1.createItemDebris(secretNote, new Vector2(xLocation + 0.5f, yLocation + 0.5f) * 64f, -1);
                                return;
                            }
                        }

                        Game1.createMultipleObjectDebris(toDigUpItemID, xLocation, yLocation, random.Next(1, 4), who.UniqueMultiplayerID);
                    }
                }
            }
        }
    }
}