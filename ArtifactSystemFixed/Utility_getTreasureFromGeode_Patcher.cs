using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using System;

using static ArtifactSystemFixed.Utils;
using Object = StardewValley.Object;

namespace ArtifactSystemFixed
{
    internal class Utility_getTreasureFromGeode_Patcher : Patch
    {
        public static ModConfig Config { private get; set; } = null;

        protected override PatchDescriptor GetPatchDescriptor()
        {
            return new PatchDescriptor(new Utility().GetType(), "getTreasureFromGeode");
        }

        public static bool Prefix()
        {
            return false;
        }

        public static Object Postfix(Object o, Item geode)
        {
            var random = new Random((int) Game1.stats.GeodesCracked + (int) Game1.uniqueIDForThisGame / 2);
            var whichGeode = ((Object) geode).ParentSheetIndex;

            if (random.NextDouble() < 0.5)
            {
                #region Choose ore/basic object (unchanged from vanilla)

                var amount = random.Next(3) * 2 + 1;
                if (random.NextDouble() < 0.1)
                    amount = 10;
                if (random.NextDouble() < 0.01)
                    amount = 20;

                if (random.NextDouble() < 0.5)
                {
                    switch (random.Next(4))
                    {
                        case 0:
                        case 1: return new Object(390, amount); //Stone
                        case 2: return new Object(330, 1); //Clay
                        case 3:
                        {
                            int parentSheetIndex;
                            switch (whichGeode)
                            {
                                case 749: //Omni geode
                                    parentSheetIndex = 82 + random.Next(3) * 2; // 82/84/86 : FireQuartz/FrozenTear/EarthCrystal
                                break;
                                case 536: //Frozen Geode
                                    parentSheetIndex = 84; //Frozen Tear
                                break;
                                case 535: //Basic geode
                                    parentSheetIndex = 86; //Earth Crystal
                                break;
                                default: //Logically only Magma Geode
                                    parentSheetIndex = 82; //Fire Quartz
                                break;
                            }

                            return new Object(parentSheetIndex, 1);
                        }
                    }
                }
                else
                {
                    #region Choose ore

                    switch (whichGeode)
                    {
                        case 535: //Basic geode
                            switch (random.Next(3))
                            {
                                case 0:
                                    return new Object(378, amount); //Copper ore
                                case 1:
                                    return new Object(Game1.player.deepestMineLevel > 25 ? 380 : 378,
                                        amount); // Iron or Copper ore
                                case 2:
                                    return new Object(382, amount); //Coal
                            }

                        break;
                        case 536: //Frozen geode
                            switch (random.Next(4))
                            {
                                case 0:
                                    return new Object(378, amount); //Copper ore
                                case 1:
                                    return new Object(380, amount); //Iron ore
                                case 2:
                                    return new Object(382, amount); //Coal
                                case 3:
                                    return new Object(Game1.player.deepestMineLevel > 75 ? 384 : 380,
                                        amount); //Gold or Iron ore
                            }
                        break;
                        default: //Omni or magma geode
                            switch (random.Next(5))
                            {
                                case 0:
                                    return new Object(378, amount); //Copper ore
                                case 1:
                                    return new Object(380, amount); //Iron ore
                                case 2:
                                    return new Object(382, amount); //Coal
                                case 3:
                                    return new Object(384, amount); //Gold ore
                                case 4:
                                    return new Object(386, amount / 2 + 1); //Iridium ore
                            }
                        break;
                    }

                    #endregion
                }

                return new Object(Vector2.Zero, 390, 1); //Stone

                #endregion
            }
            //Choose treasure

            if (Math.Abs(Config.Geode_AlreadyFoundMultiplier) > 0.01)
            {
                #region Weighted probability

                Console.WriteLine("Choosing geode with weighted probability");

                var treasures = Game1.objectInformation[whichGeode].Split('/')[6].Split(' '); //e.g. 539 540 543 547 553 554 562 563 565 570 575 578 122

                var treasuresToWeight = new Dictionary<int, double>();
                foreach (var treasureString in treasures)
                    if (int.TryParse(treasureString, out var treasure))
                    {
                        var timesFound = 0;
                        var type = Game1.objectInformation[treasure].Split('/')[3];

                        if (type.Contains("Mineral"))
                            timesFound = GetNumberOfMineralFound(treasure);
                        else if (type.Contains("Arch"))
                            timesFound = GetNumberOfArtifactFound(treasure);

                        var weight = Math.Pow(Config.Geode_AlreadyFoundMultiplier, timesFound);

                        treasuresToWeight.Add(treasure, weight);
                    }

                var index = ChooseWeightedProbability(treasuresToWeight, random, 390); //It is possible to have nothing to choose if weight multiplier is 0
                Console.WriteLine($"Weighted probability chose {index}");

                if (whichGeode == 749 && random.NextDouble() < 0.008 && (int) Game1.stats.GeodesCracked > 15)
                    index = 74; //Prismatic shard

                return new Object(index, 1);

                #endregion
            }
            else
            {
                #region Vanilla selection

                var treasures = Game1.objectInformation[whichGeode].Split('/')[6].Split(' '); //e.g. 539 540 543 547 553 554 562 563 565 570 575 578 122
                var index = Convert.ToInt32(treasures[random.Next(treasures.Length)]);

                if (whichGeode == 749 && random.NextDouble() < 0.008 && (int) Game1.stats.GeodesCracked > 15)
                    index = 74; //Prismatic shard

                return new Object(index, 1);

                #endregion
            }
        }
    }
}