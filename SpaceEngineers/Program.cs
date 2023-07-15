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
                (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
                _drawingSurface.SurfaceSize
            );

            PrepareTextSurfaceForSprites(_drawingSurface, _drawingSurfaceProduction);
        }

        public void Main(string argument, UpdateType updateType)
        {
            var frame = _drawingSurface.DrawFrame();
            var prodFrame = _drawingSurfaceProduction.DrawFrame();

            DrawSprites(ref frame, ref prodFrame);

            prodFrame.Dispose();
            frame.Dispose();
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
                Data = AllItems()[0],
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
                Data = AllItems()[1],
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
                Data = AllItems()[2],
                Position = componentPosition,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.LEFT /* Center the text on the position */,
                FontId = "White"
            };

            frame.Add(sprite);

            var statusPosition = new Vector2(800, 20) + _viewport.Position;

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

        List<string> AllItems()
        {
            IDictionary<string, int> oreDict = new Dictionary<string, int>();
            IDictionary<string, int> ingotDict = new Dictionary<string, int>();
            IDictionary<string, int> componentDict = new Dictionary<string, int>();

            oreDict = initOreDict(oreDict);
            ingotDict = initIngotDict(ingotDict);
            componentDict = initComponenttDict(componentDict);

            List<string> returns = new List<string>();

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
                        oreDict[name] += amount; 
                    }
                    else if (s.Substring(found + 1, 5) == "Ingot")
                    {
                        ingotDict[name] += amount;
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

            returns.Add(string.Join("", oreDict.Select(pair => string.Format("{0}: {1}\n", pair.Key.ToString(), pair.Value.ToString())).ToArray()));
            returns.Add(string.Join("", ingotDict.Select(pair => string.Format("{0}: {1}\n", pair.Key.ToString(), pair.Value.ToString())).ToArray()));
            returns.Add(string.Join("", componentDict.Select(pair => string.Format("{0}: {1}\n", pair.Key.ToString(), pair.Value.ToString())).ToArray()));

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
            int activeAssemblers = 0;

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

            return assemblers + activeAssemblers + "/" + assemblerList.Count + "\n";
        }

        public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface, IMyTextSurface textSurfaceProduction)
        {

            textSurface.ContentType = ContentType.SCRIPT;
            textSurface.Script = "";

            textSurfaceProduction.ContentType = ContentType.SCRIPT;
            textSurfaceProduction.Script = "";
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

    }
}
