using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using static Sandbox.Game.Weapons.MyDrillBase;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        IMyTextSurface _drawingSurface;
        IMyTextSurface _drawingSurfaceProduction;
        RectangleF _viewport;
        RectangleF _viewportProduction;
        IDictionary<string, int> componentStockDict;
        IDictionary<string, string> componentMap;

        public Program()
        {
            _drawingSurface = GridTerminalSystem.GetBlockWithName("InventoryPanel") as IMyTextSurface;
            _drawingSurfaceProduction = GridTerminalSystem.GetBlockWithName("ProductionPanel") as IMyTextSurface;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _viewport = new RectangleF(
                (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
                _drawingSurface.SurfaceSize
            );
            _viewportProduction = new RectangleF(
                (_drawingSurfaceProduction.TextureSize - _drawingSurfaceProduction.SurfaceSize) / 2f,
                _drawingSurfaceProduction.SurfaceSize
            );

            PrepareTextSurfaceForSprites(_drawingSurface, _drawingSurfaceProduction);
        }

        public void Main(string argument, UpdateType updateType)
        {
            var frame = _drawingSurface.DrawFrame();
            var prodFrame = _drawingSurfaceProduction.DrawFrame();

            componentStockDict = initComponentStock();
            componentMap = initComponentMap();

            DrawSprites(ref frame, ref prodFrame);

            frame.Dispose();
            prodFrame.Dispose();
        }

        public void DrawSprites(ref MySpriteDrawFrame frame, ref MySpriteDrawFrame prodFrame)
        {
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "White screen",
                Position = _viewport.Center,
                Size = _viewport.Size,
                Color = Color.Black,
                Alignment = TextAlignment.CENTER
            };

            frame.Add(sprite);
            prodFrame.Add(sprite);

            var orePosition = new Vector2(10, 20) + _viewport.Position;

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = string.Join("", AllItems()[0].Select(pair => string.Format("{0}: {1}\n", pair.Key.ToString(), pair.Value.ToString())).ToArray()),
                Position = orePosition,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.LEFT /* Center the text on the position */,
                FontId = "White"
            };

            frame.Add(sprite);

            var ingotPosition = new Vector2(250, 20) + _viewport.Position;

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = string.Join("", AllItems()[1].Select(pair => string.Format("{0}: {1}\n", pair.Key.ToString(), pair.Value.ToString())).ToArray()),
                Position = ingotPosition,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.LEFT /* Center the text on the position */,
                FontId = "White"
            };

            frame.Add(sprite);

            var componentPosition = new Vector2(490, 20) + _viewport.Position;

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = string.Join("", AllItems()[2].Select(pair => string.Format("{0}: {1}\n", pair.Key.ToString(), pair.Value.ToString())).ToArray()),
                Position = componentPosition,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.LEFT /* Center the text on the position */,
                FontId = "White"
            };

            frame.Add(sprite);

            var statusPosition = new Vector2(850, 20) + _viewport.Position;

            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = productionStatus().ToString(),
                Position = statusPosition,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.LEFT /* Center the text on the position */,
                FontId = "White"
            };

            frame.Add(sprite);
        }

        List<IDictionary<string, int>> AllItems()
        {
            IDictionary<string, int> oreDict = new Dictionary<string, int>();
            IDictionary<string, int> ingotDict = new Dictionary<string, int>();
            IDictionary<string, int> componentDict = new Dictionary<string, int>();

            oreDict = initOreDict(oreDict);
            ingotDict = initIngotDict(ingotDict);
            componentDict = initComponenttDict(componentDict);

            List<IDictionary<string, int>> returns = new List<IDictionary<string, int>>();

            List<IMyTerminalBlock> cargosList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargosList);
            
            for (int x = 0; x < cargosList.Count; x++)
            {
                IMyCargoContainer cargo = cargosList[x] as IMyCargoContainer;

                List<MyInventoryItem> items = new List<MyInventoryItem>();
                cargo.GetInventory(0).GetItems(items);

                for (int i = 0; i < items.Count; i++)
                {
                    string s = items[i].Type.ToString();
                    int amount = (int)items[i].Amount;
                    int found = s.IndexOf("_");
                    string name = s.Substring(found + 1);

                    if (s.Substring(found + 1, 3) == "Ore")
                    {
                        if (oreDict.ContainsKey(name))
                        {
                            oreDict[name] += amount;
                        }
                    }
                    else if (s.Substring(found + 1, 5) == "Ingot")
                    {
                        if (ingotDict.ContainsKey(name))
                        {
                            ingotDict[name] += amount;
                        }
                    }
                    else if (s.Substring(found + 1, 9) == "Component")
                    {
                        if (componentDict.ContainsKey(name))
                        {
                            componentDict[name] += amount;
                        }
                    }
                }
            }

            returns.Add(oreDict);
            returns.Add(ingotDict);
            returns.Add(componentDict);

            return returns;
        }

        string productionStatus()
        {
            string status = "";

            status += getRefineryStatus();
            status += getAssemblerStatus();

            return status;
        }

        string getRefineryStatus()
        {
            string refineries = "Refineries: ";
            int activeRefineries = 0;

            List<IMyTerminalBlock> refineryList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refineryList);

            for (int i = 0; i < refineryList.Count; i++)
            {

                IMyRefinery refinery = refineryList[i] as IMyRefinery;
                if (refinery.IsProducing)
                {
                    activeRefineries++;
                }
            }

            return refineries + activeRefineries + "/" + refineryList.Count + "\n";
        }

        string getAssemblerStatus()
        {
            string assemblers = "Assemblers: ";
            IDictionary<string, int> myItems = AllItems()[2];

            int activeAssemblers = 0;
            HashSet<string> productionValues = new HashSet<string>();
            List<MyProductionItem> items = new List<MyProductionItem>();

            List<IMyTerminalBlock> assemblerList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblerList);

            for (int i = 0; i < assemblerList.Count; i++)
            {

                IMyAssembler assembler = assemblerList[i] as IMyAssembler;
                if (assembler.IsProducing)
                {
                    activeAssemblers++;
                }
            }

            foreach (var comp in myItems)
            {
                if (componentMap.ContainsKey(comp.Key))
                {
                    if (componentStockDict.ContainsKey(componentMap[comp.Key]))
                    {
                        if (comp.Value < componentStockDict[componentMap[comp.Key]])
                        {
                            IMyAssembler compAssembler = GridTerminalSystem.GetBlockWithName(comp.Key) as IMyAssembler;
                            if (!compAssembler.IsProducing & compAssembler.IsQueueEmpty)
                            {
                                compAssembler.AddQueueItem(MyDefinitionId.Parse(componentMap[comp.Key]), Convert.ToDouble(componentStockDict[componentMap[comp.Key]] - comp.Value));
                            }
                        }
                    }
                }
            }

            return assemblers + activeAssemblers + "/" + assemblerList.Count + "\n";
        }

        public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface, IMyTextSurface textProductionSurface)
        {

            textSurface.ContentType = ContentType.SCRIPT;
            textSurface.Script = "";

            textProductionSurface.ContentType = ContentType.SCRIPT;
            textProductionSurface.Script = "";
        }

        IDictionary<string, int> initOreDict(IDictionary<string, int> oreDict)
        {
            oreDict.Add("Ore/Cobalt", 0);
            oreDict.Add("Ore/Gold", 0);
            oreDict.Add("Ore/Iron", 0);
            oreDict.Add("Ore/Magnesium", 0);
            oreDict.Add("Ore/Nickel", 0);
            oreDict.Add("Ore/Platinum", 0);
            oreDict.Add("Ore/Silicon", 0);
            oreDict.Add("Ore/Silver", 0);
            oreDict.Add("Ore/Stone", 0);
            oreDict.Add("Ore/Uranium", 0);

            return oreDict;
        }

        IDictionary<string, int> initIngotDict(IDictionary<string, int> ingotDict)
        {
            ingotDict.Add("Ingot/Cobalt", 0);
            ingotDict.Add("Ingot/Gold", 0);
            ingotDict.Add("Ingot/Iron", 0);
            ingotDict.Add("Ingot/Magnesium", 0);
            ingotDict.Add("Ingot/Nickel", 0);
            ingotDict.Add("Ingot/Platinum", 0);
            ingotDict.Add("Ingot/Silicon", 0);
            ingotDict.Add("Ingot/Silver", 0);
            ingotDict.Add("Ingot/Stone", 0);
            ingotDict.Add("Ingot/Uranium", 0);

            return ingotDict;
        }

        IDictionary<string, int> initComponenttDict(IDictionary<string, int> componentDict)
        {
            componentDict.Add("Component/BulletproofGlass", 0);
            componentDict.Add("Component/Computer", 0);
            componentDict.Add("Component/Construction", 0);
            componentDict.Add("Component/Detector", 0);
            componentDict.Add("Component/Display", 0);
            componentDict.Add("Component/Girder", 0);
            componentDict.Add("Component/InteriorPlate", 0);
            componentDict.Add("Component/LargeTube", 0);
            componentDict.Add("Component/MetalGrid", 0);
            componentDict.Add("Component/Motor", 0);
            componentDict.Add("Component/PowerCell", 0);
            componentDict.Add("Component/RadioCommunication", 0);
            componentDict.Add("Component/Reactor", 0);
            componentDict.Add("Component/SmallTube", 0);
            componentDict.Add("Component/SolarCell", 0);
            componentDict.Add("Component/SteelPlate", 0);
            componentDict.Add("Component/Superconductor", 0);
            componentDict.Add("Component/Thrust", 0);

            return componentDict;
        }

        IDictionary<string, int> initComponentStock()
        {
            IDictionary<string, int> componentStock = new Dictionary<string, int>();
            
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/BulletproofGlass", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/ComputerComponent", 5000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/ConstructionComponent", 10000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/DetectorComponent", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/Display", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/GirderComponent", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/InteriorPlate", 10000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/LargeTube", 5000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/MetalGrid", 5000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/MotorComponent", 5000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/PowerCell", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent", 1000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/ReactorComponent", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/SmallTube", 5000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/SolarCell", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/SteelPlate", 15000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/Superconductor", 3000);
            componentStock.Add("MyObjectBuilder_BlueprintDefinition/ThrustComponent", 3000);
          
            return componentStock;
        }

        IDictionary<string, string> initComponentMap()
        {
            IDictionary<string, string> componentMap = new Dictionary<string, string>();
            
            componentMap.Add("Component/BulletproofGlass", "MyObjectBuilder_BlueprintDefinition/BulletproofGlass");
            componentMap.Add("Component/Computer", "MyObjectBuilder_BlueprintDefinition/ComputerComponent");
            componentMap.Add("Component/Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent");
            componentMap.Add("Component/Detector", "MyObjectBuilder_BlueprintDefinition/DetectorComponent");
            componentMap.Add("Component/Display", "MyObjectBuilder_BlueprintDefinition/Display");
            componentMap.Add("Component/Girder", "MyObjectBuilder_BlueprintDefinition/GirderComponent");
            componentMap.Add("Component/InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate");
            componentMap.Add("Component/LargeTube", "MyObjectBuilder_BlueprintDefinition/LargeTube");
            componentMap.Add("Component/MetalGrid", "MyObjectBuilder_BlueprintDefinition/MetalGrid");
            componentMap.Add("Component/Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent");
            componentMap.Add("Component/PowerCell", "MyObjectBuilder_BlueprintDefinition/PowerCell");
            componentMap.Add("Component/RadioCommunication", "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent");
            componentMap.Add("Component/Reactor", "MyObjectBuilder_BlueprintDefinition/ReactorComponent");
            componentMap.Add("Component/SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube");
            componentMap.Add("Component/SolarCell", "MyObjectBuilder_BlueprintDefinition/SolarCell");
            componentMap.Add("Component/SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate");
            componentMap.Add("Component/Superconductor", "MyObjectBuilder_BlueprintDefinition/Superconductor");
            componentMap.Add("Component/Thrust", "MyObjectBuilder_BlueprintDefinition/ThrustComponent");

            return componentMap;
        }

    }
}
