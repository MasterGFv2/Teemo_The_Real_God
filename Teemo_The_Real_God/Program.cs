using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Color = System.Drawing.Color;

namespace Teemo___The_Real_God
{
    internal class Program
    {
        public const string ChampionName = "Teemo";

        public static Orbwalking.Orbwalker Orbwalker;

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static ShroomTables ShroomPositions;

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public static Menu Config;

        public static bool Packets
        {
            get { return Config.SubMenu("Misc").Item("packets").GetValue<bool>(); }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName)
            {
                Game.PrintChat("You not playing teemo!");
                return;
            }

            Q = new Spell(SpellSlot.Q, 580);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 230);

            R.SetSkillshot(0.1f, 75f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config = new Menu("Teemo - The Real God", "Teemo - The Real God", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            var orbwalking = Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            var combo = Config.AddSubMenu(new Menu("Combo", "Combo"));
            var harass = Config.AddSubMenu(new Menu("Harass", "Harass"));
            var laneclear = Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            var misc = Config.AddSubMenu(new Menu("Misc", "Misc"));

            Config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("useR", "Use R").SetValue(true));

            harass.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));

            laneclear.AddItem(new MenuItem("clearQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("clearR", "Use R").SetValue(true));

            Orbwalker = new Orbwalking.Orbwalker(orbwalking);
            Config.AddToMainMenu();
            Config.AddItem(new MenuItem("autoQ", "Automatic Q").SetValue(false));
            Config.AddItem(new MenuItem("autoW", "Automatic W").SetValue(false));

            var interrupt = Config.AddSubMenu(new Menu("Interrupt", "Interrupt"));
            interrupt.AddItem(new MenuItem("intQ", "Use Q").SetValue(true));

            misc.AddItem(new MenuItem("packets", "Use Packets").SetValue(false));
            misc.AddItem(new MenuItem("autoR", "Auto Place Mushrooms").SetValue(true));

            var ks = Config.AddSubMenu(new Menu("Kill Steal Menu", "Kill Steal Menu"));
            ks.AddItem(new MenuItem("ksQ", "Use Q").SetValue(true));

            var drawing = Config.AddSubMenu(new Menu("Drawing", "Drawing"));
            drawing.AddItem(new MenuItem("drawQ", "Draw Q Range").SetValue(false));
            drawing.AddItem(new MenuItem("drawR", "Draw R Range").SetValue(false));
            drawing.AddItem(new MenuItem("drawautoR", "Draw Important Shroom Areas").SetValue(true));
            drawing.AddItem(new MenuItem("DrawVision", "Shroom Vision").SetValue(new Slider(1500, 2500, 1000)));

            var flee = Config.AddSubMenu(new Menu("Flee Menu", "Flee"));
            flee.AddItem(new MenuItem("fleetoggle", "Flee").SetValue(new KeyBind(90, KeyBindType.Press)));
            flee.AddItem(new MenuItem("fleeW", "Use W").SetValue(true));
            flee.AddItem(new MenuItem("fleeR", "Use R").SetValue(true));

            ShroomPositions = new ShroomTables();
            Game.OnUpdate += Game_OnGameUpdate;
            Interrupter2.OnInterruptableTarget += Interrupter_OnPossibleToInterrupt;
            Drawing.OnDraw += DrawingOnOnDraw;
            Game.PrintChat("Teemo - The Real God loaded. (C)MasterGF");
            Game.PrintChat("<b>A thanks to karmapanda i learned nice stuff from his Code.</b>");
        }

        #region IsShroom

        private static bool IsShroomed(Vector3 position)
        {
            return ObjectManager.Get<Obj_AI_Base>().Where(obj => obj.Name == "Noxious Trap").Any(obj => position.Distance(obj.Position) <= 250);
        }

        #endregion

        #region Combo

        public static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = Config.SubMenu("Combo").Item("useQ").GetValue<bool>();
            var useW = Config.SubMenu("Combo").Item("useW").GetValue<bool>();
            var useR = Config.SubMenu("Combo").Item("useR").GetValue<bool>();


            if (Q.IsReady() && useQ)
            {
                Q.Cast(target, Packets);
            }

            if (W.IsReady() && useW)
            {
                W.Cast(true);
            }

            if (R.IsReady() && useR && R.IsInRange(target))
            {
                R.Cast(target.Position, Packets);
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (target.IsValidTarget())
                {
                    Orbwalking.Attack = true;
                }
                else
                {
                    Orbwalking.Attack = false;
                }
            }
            else
            {
                Orbwalking.Attack = true;
            }
        }

        #endregion

        #region KillSteal
        public static void ksQ()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null) return;

            if (Q.IsReady())
            {
                if (target.Health < Q.GetDamage(target))
                {
                    Q.Cast(target, Packets);
                }
            }
        }
        #endregion

        #region Harass

        public static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var useQ = Config.SubMenu("Harass").Item("harassQ").GetValue<bool>();

            if (!Q.IsReady() || !useQ)
            {
                return;
            }

            if (target.IsValidTarget())
            {
                Q.Cast(target, Packets);
            }
        }

        #endregion

        #region LaneClear

        public static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var rangedMinionsQ = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.Ranged);

            var allMinionsR = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, R.Range);
            var rangedMinionsR = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, R.Range, MinionTypes.Ranged);

            var rLocation = R.GetCircularFarmLocation(allMinionsR, R.Range);
            var r2Location = R.GetCircularFarmLocation(rangedMinionsR, R.Range);

            var useQ = Config.SubMenu("LaneClear").Item("clearQ").GetValue<bool>();
            var useR = Config.SubMenu("LaneClear").Item("clearR").GetValue<bool>();

            var bestLocation = (rLocation.MinionsHit > r2Location.MinionsHit + 1) ? rLocation : r2Location;

            if (allMinionsQ.Count > 0 & useQ)
            {
                if (allMinionsQ[0].Health < ObjectManager.Player.GetSpellDamage(allMinionsQ[0], SpellSlot.Q) &&
                    Q.IsReady())
                {
                    Q.CastOnUnit(allMinionsQ[0], Packets);
                }
            }

            if (!(allMinionsR.Count > 0 & useR))
            {
                return;
            }

            if (allMinionsR[0].Health < ObjectManager.Player.GetSpellDamage(allMinionsR[0], SpellSlot.R) && R.IsReady() && R.IsInRange(r2Location.Position.To3D()))
            {
                R.Cast(bestLocation.Position, true);
            }

            else if (allMinionsR[0].Health < ObjectManager.Player.GetSpellDamage(allMinionsR[0], SpellSlot.R) &&
                     R.IsReady() && R.IsInRange(rLocation.Position.To3D()))
            {
                R.Cast(bestLocation.Position, true);
            }

        }

        #endregion

        #region Interrupt

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            var intQ = Config.SubMenu("Interrupt").Item("intQ").GetValue<bool>();

            if (intQ & Q.IsReady() || args.DangerLevel != Interrupter2.DangerLevel.High)
            {
                Q.Cast(sender, Packets);
            }
        }

        #endregion

        #region AutoShroom

        private static void AutoShroom()
        {
            if (!R.IsReady())
                return;
            if (Config.SubMenu("Misc").Item("autoR").GetValue<bool>())
                foreach (var place in ShroomPositions.HighPriority.Where(pos => pos.Distance(ObjectManager.Player.Position) <= R.Range && !IsShroomed(pos)))
                    R.Cast(place, Packets);
        }

        #endregion

        #region LastHit

        public static void LastHit()
        {
            double TeemoE = 0;

            var t = TargetSelector.GetTarget((float)TeemoE, TargetSelector.DamageType.Physical);

            TeemoE += Player.GetSpellDamage(t, SpellSlot.E);

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 30, MinionTypes.All);

                foreach (var minion in allMinions)
                {
                    if (minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion) + TeemoE)
                    {
                        Orbwalking.CanAttack();
                    }
                }
            }
            else
            {
                return;
            }
        }

        #endregion

        #region Flee

        static void Flee()
        {
            var useR = Config.SubMenu("Flee").Item("fleeR").GetValue<bool>();
            var useW = Config.SubMenu("Flee").Item("fleeW").GetValue<bool>();

            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (useR && R.IsReady())
            {
                R.Cast(Player.Position, Packets);
            }

            if (useW && W.IsReady())
            {
                W.Cast(Player);
            }
        }

        #endregion

        private static void Game_OnGameUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            AutoShroom();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harass();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                Orbwalking.DisableNextAttack = true;
                LastHit();
            }

            //KillSteal
            if (Config.SubMenu("Kill Steal Menu").Item("ksQ").GetValue<bool>())
            {
                ksQ();
            }

            // Debug
            //if(Config.SubMenu("Console").Item("Debug").IsActive())
            //{
            //    Console.WriteLine(Player.Position.X + "is the X position");
            //    Console.WriteLine(Player.Position.Y + "is the Y Position");
            //    Console.WriteLine(Player.Position.Z + "is the Z Position");
            //}

            //Flee Menu
            if (Config.SubMenu("Flee").Item("fleetoggle").IsActive())
            {
                Flee();
            }
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = Config.SubMenu("Drawing").Item("drawQ").GetValue<bool>();
            var drawR = Config.SubMenu("Drawing").Item("drawR").GetValue<bool>();
            var drawautoR = Config.SubMenu("Drawing").Item("drawautoR").GetValue<bool>();

            var player = ObjectManager.Player.Position;

            if (drawQ)
            {
                Render.Circle.DrawCircle(player, Q.Range, Q.IsReady() ? System.Drawing.Color.Gold : System.Drawing.Color.Green);
            }
            if (drawR)
            {
                Render.Circle.DrawCircle(player, R.Range, R.IsReady() ? System.Drawing.Color.Gold : System.Drawing.Color.Green);
            }
            if (drawautoR)
                foreach (var place in ShroomPositions.HighPriority.Where(pos => pos.Distance(ObjectManager.Player.Position) <= Config.SubMenu("Drawing").Item("DrawVision").GetValue<Slider>().Value))
                {
                    Render.Circle.DrawCircle(place, 100, System.Drawing.Color.Red);
                }
        }
        //The following code is taken from UC2's Teemo. All the shroom locations are modified in the recent update for better positioning.
        //To add a shroom location, press T and look at the console for the X Y Z positions and copy the template below to add your own location.
        internal class ShroomTables
        {
            public List<Vector3> HighPriority = new List<Vector3>();

            public ShroomTables()
            {
                CreateTables();
                var list = (from pos in HighPriority
                            let x = pos.X
                            let y = pos.Y
                            let z = pos.Z
                            select new Vector3(x, z, y)).ToList();
                HighPriority = list;
            }
            private void CreateTables()
            {
                //Top Lane Blue Side including Baron
                HighPriority.Add(new Vector3(2790f, 50.16358f, 7278f));
                HighPriority.Add(new Vector3(3700.708f, -11.22648f, 9294.094f));
                HighPriority.Add(new Vector3(2314f, 53.165f, 9722f));
                HighPriority.Add(new Vector3(3090f, -68.03732f, 10810f));
                HighPriority.Add(new Vector3(4722f, -71.2406f, 10010f));
                HighPriority.Add(new Vector3(5208f, -71.2406f, 9114f));
                HighPriority.Add(new Vector3(4724f, 52.53909f, 7590f));
                HighPriority.Add(new Vector3(4564f, 51.83786f, 6060f));
                HighPriority.Add(new Vector3(2760f, 52.96445f, 5178f));
                HighPriority.Add(new Vector3(4440f, 56.8484f, 11840f));

                //Top Lane Tri Bush
                HighPriority.Add(new Vector3(2420f, 52.8381f, 13482f));
                HighPriority.Add(new Vector3(1630f, 52.8381f, 13008f));
                HighPriority.Add(new Vector3(1172f, 52.8381f, 12302f));

                //Top Lane Red Side
                HighPriority.Add(new Vector3(5666f, 52.8381f, 12722f));
                HighPriority.Add(new Vector3(8004f, 56.4768f, 11782f));
                HighPriority.Add(new Vector3(9194f, 53.35013f, 11368f));
                HighPriority.Add(new Vector3(8280f, 50.06194f, 10254f));

                //Red Buff bush Red Side
                HighPriority.Add(new Vector3(6728f, 53.82967f, 11450f));


                //Template
                //HighPriority.Add(new Vector3("X"f, "Z"f, "Y"f));
            }
        }
    }
}