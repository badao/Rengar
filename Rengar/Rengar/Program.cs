using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace Rengar
{
    class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Orbwalking.Orbwalker Orbwalker;

        private static Spell Q, W, E, R, summoner1, summoner2;

        private static Menu Menu;

        private static string mode { get { return Menu.Item("ComboMode").GetValue<StringList>().SelectedValue; } }
        private static string youmumu { get { return Menu.Item("Youmumu").GetValue<StringList>().SelectedValue; } }
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Rengar")
                return;

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W,300);
            E = new Spell(SpellSlot.E,1000);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
            E.MinHitChance = HitChance.Medium;
            W.SetSkillshot(0.25f, 500, 2000, false, SkillshotType.SkillshotCircle);
            W.MinHitChance = HitChance.Medium;
            foreach (var spell in
                        Player.Spellbook.Spells.Where(
                          i =>
                                i.Name.ToLower().Contains("smite") &&
            (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2)))
            {
                SmiteSlot = spell.Slot;
            }
            //Q.SetSkillshot(300, 50, 2000, false, SkillshotType.SkillshotLine);


            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Rengar.Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            var ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);

            var spellMenu = Menu.AddSubMenu(new Menu("Spells", "Spells"));
            spellMenu.AddItem(new MenuItem("ComboSwitch", "ComboModeSwitch").SetValue(new KeyBind("T".ToCharArray()[0],KeyBindType.Press)));
            spellMenu.AddItem(new MenuItem("ComboMode", "ComboMode").SetValue(new StringList(new[] { "Snare", "Burst","Auto","Always Q"},0)));
            spellMenu.AddItem(new MenuItem("useSmite", "Use Smite Combo").SetValue(true));
            spellMenu.AddItem(new MenuItem("useYoumumu", "Use Youmumu while Steath").SetValue(true));
            spellMenu.AddItem(new MenuItem("Youmumu", "Youmumu while steath mode").SetValue(new StringList(new[] { "Always", "ComboMode" }, 0)));
            //spellMenu.AddItem(new MenuItem("DontWaitReset","Dont Wait Reset AA with Q").SetValue(true));
            var clear = spellMenu.AddSubMenu(new Menu("Clear","Clear"));
            clear.AddItem(new MenuItem("useQ", "use Q").SetValue(true));
            clear.AddItem(new MenuItem("useE", "use E").SetValue(true));
            clear.AddItem(new MenuItem("useW", "use W").SetValue(true));
            clear.AddItem(new MenuItem("Save", "Save 5  FEROCITY").SetValue(false));
            var auto = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            auto.AddItem(new MenuItem("AutoHeal","Auto W if HP <").SetValue(new Slider(20,0,100)));
            auto.AddItem(new MenuItem("AutoSmite", "Auto Smite Heal if HP <").SetValue(new Slider(20, 0, 100)));
            auto.AddItem(new MenuItem("Interrupt", "Interrupt with E").SetValue(true));
            auto.AddItem(new MenuItem("SmiteKS", "Smite KillSteal").SetValue(true));
            auto.AddItem(new MenuItem("SmiteSteal", "Smite Steal Baron Dragon").SetValue(true));
            var Drawing = Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Drawing.AddItem(new MenuItem("DrawMode", "Draw Combo Mode").SetValue(true));
            Drawing.AddItem(new MenuItem("Notify", "Notify Selected Target").SetValue(true));
            Menu.AddToMainMenu();

            //Drawing.OnDraw += Drawing_OnDraw;

            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += AfterAttack;
            Orbwalking.OnAttack += OnAttack;
            Obj_AI_Base.OnProcessSpellCast += oncast;
            CustomEvents.Unit.OnDash += Unit_OnDash;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            LeagueSharp.Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && !Player.HasBuff("rengarpassivebuff") && Q.IsReady() && !(mode == "Snare" && Player.Mana == 5))
            {
                var x = Prediction.GetPrediction(args.Target as Obj_AI_Base,Player.AttackCastDelay + 0.04f);
                if (Player.Position.To2D().Distance(x.UnitPosition.To2D()) 
                    >= Player.BoundingRadius + Player.AttackRange + args.Target.BoundingRadius)
                {
                    args.Process = false;
                    Q.Cast();
                }
            }
        }

        private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (!sender.IsMe)
                return;
            if ( args.Buff.Name == "rengarqbase" ||args.Buff.Name == "rengarqemp" )
            {

            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Menu.Item("DrawMode").GetValue<bool>())
            {
                var x = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(x[0], x[1], Color.White, mode);
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Interrupt && Player.Mana == 5 && E.IsReady())
            {
                if (sender.IsValidTarget(E.Range))
                {
                    E.Cast(sender);
                }
            }
        }
        private static bool notify { get { return Menu.Item("Notify").GetValue<bool>(); } }
        //private static bool dontwaitQ { get { return Menu.Item("DontWaitReset").GetValue<bool>(); } }
        private static int autoSmiteHeal { get { return Menu.Item("AutoSmite").GetValue<Slider>().Value; } }
        private static bool useSmiteSteal { get { return Menu.Item("SmiteSteal").GetValue<bool>(); } }
        private static bool useSmiteKS { get { return Menu.Item("SmiteKS").GetValue<bool>(); } }
        private static bool useSmiteCombo { get { return Menu.Item("useSmite").GetValue<bool>(); } }
        private static bool useyoumumu { get { return Menu.Item("useYoumumu").GetValue<bool>(); } }
        private static bool useQ { get { return Menu.Item("useQ").GetValue<bool>(); } }
        private static bool useE { get { return Menu.Item("useE").GetValue<bool>(); } }
        private static bool useW { get { return Menu.Item("useW").GetValue<bool>(); } }
        private static bool Save { get { return Menu.Item("Save").GetValue<bool>(); } }
        private static int Heal { get { return Menu.Item("AutoHeal").GetValue<Slider>().Value; } }
        private static bool Interrupt { get { return Menu.Item("Interrupt").GetValue<bool>(); } }
        private static int extrawindup { get { return Orbwalking.Orbwalker._config.Item("ExtraWindup").GetValue<Slider>().Value; } }
        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            //if (hasSmite)
            //{
            //    Game.PrintChat(hasSmiteBlue .ToString());
            //}
            DrawSelectedTarget();
            ComboModeSwitch();
            //checkbuff();
            KillSteall();
            if (ItemData.Youmuus_Ghostblade.GetItem().IsReady() && youmumu == "Always" && Player.HasBuff("RengarR") && useyoumumu)
            {
                ItemData.Youmuus_Ghostblade.GetItem().Cast();
            }
            if(Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                combo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                clear();
            }
        }

        public static void OnAttack(AttackableUnit unit, AttackableUnit target)
        {

            if (unit.IsMe && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (ItemData.Youmuus_Ghostblade.GetItem().IsReady())
                    ItemData.Youmuus_Ghostblade.GetItem().Cast();
            }
        }
        public static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (mode == "Snare" && Player.Mana == 5)
                {
                    if (HasItem())
                        CastItem();
                }
                else if (Q.IsReady())
                {
                    Q.Cast();
                }
                else if (HasItem())
                {
                    CastItem();
                }
                else if (E.IsReady())
                {
                    var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                    {
                        E.Cast(targetE);
                    }
                    foreach (var tar in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                    {
                        if (E.IsReady())
                            E.Cast(tar);
                    }
                }
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (Player.Mana < 5 || (Player.Mana == 5 && !Save))
                {
                    if (Q.IsReady() && useQ)
                    {
                        Q.Cast();
                    }
                    else
                    {
                        if (HasItem())
                            CastItem();
                    }
                }
                else
                {
                    if (HasItem())
                        CastItem();
                }
            }
        }
        public static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spell = args.SData;
            if (!sender.IsMe)
                return;
            //Game.PrintChat(spell.Name);
            //Game.Say(spell.Name);
            if (spell.Name.ToLower().Contains("rengarq"))
            {
                //Game.PrintChat("reset");
                Orbwalking.ResetAutoAttackTimer();
            }
            //if (spell.Name.ToLower().Contains("rengarw")) ;
            if (spell.Name.ToLower().Contains("rengare"))
                if (Orbwalking.LastAATick < Utils.GameTimeTickCount - Game.Ping / 2 && Utils.GameTimeTickCount - Game.Ping / 2 < Orbwalking.LastAATick + Player.AttackCastDelay * 1000 + 40)
                {
                    DelayAction.Remove(1);
                    Orbwalking.ResetAutoAttackTimer();
                }
        }
        public static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!sender.IsMe)
                return;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && HasItem())
            {
                if (args.Duration - 100 - Game.Ping / 2 > 0)
                {
                    Utility.DelayAction.Add(
                                   (int)(/*Player.AttackCastDelay * 1000 + */args.Duration - 100 - Game.Ping / 2), CastItem);
                }
                else
                {
                    CastItem();
                }
            }
            //Game.Say("dash");
        }


        public static void combo()
        {
            if (hasSmite && useSmiteCombo && SmiteSlot.IsReady())
            {
                if (hasSmiteBlue || hasSmiteRed)
                {
                    var target = TargetSelector.GetTarget(650, TargetSelector.DamageType.Physical);
                    if (target.IsValidTarget() && !target.IsZombie && Player.Distance(target.Position) <= Player.BoundingRadius + 500 + target.BoundingRadius)
                    {
                        Player.Spellbook.CastSpell(SmiteSlot, target);
                    }
                }
            }
            
            if (ItemData.Youmuus_Ghostblade.GetItem().IsReady() && youmumu == "ComboMode" && Player.HasBuff("RengarR") && useyoumumu)
            {
                ItemData.Youmuus_Ghostblade.GetItem().Cast();
            }
            if (!Player.HasBuff("RengarR"))
            {
                if (mode == "Snare")
                {
                    if (Player.Mana < 5)
                    {
                        var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                        if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                        {
                            W.Cast(targetW);
                        }
                        if (Player.IsDashing() || Orbwalking.CanMove(extrawindup) 
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x=> x.IsValidTarget() && Orbwalking.InAutoAttackRange(x))))
                        {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack() /*&& dontwaitQ*/)
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (Player.IsDashing() || Orbwalking.CanMove(extrawindup)
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x => x.IsValidTarget() && Orbwalking.InAutoAttackRange(x))))
                        {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                    }
                }
                else if (mode == "Burst")
                {
                    if (Player.Mana < 5)
                    {
                        var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                        if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                        {
                            W.Cast(targetW);
                        }
                        if (Player.IsDashing() || Orbwalking.CanMove(extrawindup) 
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x=> x.IsValidTarget() && Orbwalking.InAutoAttackRange(x))))
                        {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack() /*&& dontwaitQ*/)
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                        if (Q.IsReady() && Player.IsDashing())
                        {
                            Q.Cast();
                        }

                        if (Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) == 0 && !Player.HasBuff("rengarpassivebuff") && !Player.IsDashing()
                            && (Player.IsDashing() || Orbwalking.CanMove(extrawindup)
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x => x.IsValidTarget() && Orbwalking.InAutoAttackRange(x)))))
                            {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                    }
                }
                else if (mode == "Auto")
                {
                    if (Player.Mana < 5)
                    {
                        var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                        if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                        {
                            W.Cast(targetW);
                        }
                        if (Player.IsDashing() || Orbwalking.CanMove(extrawindup)
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x => x.IsValidTarget() && Orbwalking.InAutoAttackRange(x))))
                        {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack() /*&& dontwaitQ*/)
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack())
                            {
                                Q.Cast();
                            }

                        }
                        if (Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) == 0 && !Player.HasBuff("rengarpassivebuff")
                            && (Player.IsDashing() || Orbwalking.CanMove(extrawindup) 
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x=> x.IsValidTarget() && Orbwalking.InAutoAttackRange(x)))))
                        {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                    }
                }
                else if (mode == "Always Q") 
                {
                    if (Player.Mana < 5)
                    {
                        var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                        if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                        {
                            W.Cast(targetW);
                        }
                        if (Player.IsDashing() || Orbwalking.CanMove(extrawindup)
                            && !(Orbwalking.CanAttack() && HeroManager.Enemies.Any(x => x.IsValidTarget() && Orbwalking.InAutoAttackRange(x))))
                        {
                            var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                            if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                            {
                                E.Cast(targetE);
                            }
                            foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                            {
                                if (E.IsReady())
                                    E.Cast(target);
                            }
                        }
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack() /*&& dontwaitQ*/)
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                        {
                            if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                        if (Q.IsReady() && Player.IsDashing())
                        {
                            Q.Cast();
                        }
                    }
                }

                else Game.Say("stupid");
            }
        }
        public static void clear()
        {
            if (Player.Mana < 5 || (Player.Mana == 5 && !Save))
            {
                var targetW1 = MinionManager.GetMinions(Player.Position, 500, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var targetE1 = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).FirstOrDefault();
                var targetW2 = MinionManager.GetMinions(Player.Position, 500, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                var targetE2 = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();
                if (W.IsReady() && targetW1 != null && useW)
                {
                    W.Cast(targetW1);
                }
                if (W.IsReady() && targetW2 != null && useW)
                {
                    W.Cast(targetW2);
                }
                if (E.IsReady() && targetE1 != null && useE)
                {
                    E.Cast(targetE1);
                }
                if (E.IsReady() && targetE2 != null && useE)
                {
                    E.Cast(targetE2);
                }
            }
        }
        public static void KillSteall()
        {
            if (Player.Health*100/Player.MaxHealth <= Heal && Player.Mana == 5 && W.IsReady())
            {
                W.Cast();
            }
            if (W.IsReady() && Player.Mana != 5)
            {
                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(500) && !x.IsZombie))
                {
                    if (target.Health <= W.GetDamage(target))
                        W.Cast(target);
                }
            }
            if (E.IsReady())
            {
                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                {
                    if (target.Health <= W.GetDamage(target))
                        E.Cast(target);
                }
            }
            if (hasSmite && useSmiteSteal)
            {
                if (SmiteSlot.IsReady())
                {
                    var creep = MinionManager.GetMinions(800, MinionTypes.All, MinionTeam.Neutral).Where(x => x.CharData.BaseSkinName == "SRU_Dragon" || x.CharData.BaseSkinName == "SRU_Baron");
                    foreach (var x in creep.Where(y => Player.Distance(y.Position) <= Player.BoundingRadius + 500 + y.BoundingRadius))
                    {
                        if (x != null && x.Health <= SmiteDamage)
                            Player.Spellbook.CastSpell(SmiteSlot, x);
                    }
                }
            }
            if (hasSmite && useSmiteKS)
            {
                if (SmiteSlot.IsReady())
                {
                    if (hasSmiteBlue || hasSmiteRed)
                    {
                        var hero = HeroManager.Enemies.Where(x => x.IsValidTarget(800) && !x.IsZombie);
                        foreach (var x in hero.Where(y => Player.Distance(y.Position) <= Player.BoundingRadius + 500 + y.BoundingRadius))
                        {
                            if (hasSmiteBlue && x != null && x.Health <= SmiteBlueDamage)
                                Player.Spellbook.CastSpell(SmiteSlot, x);
                            if (hasSmiteRed && x != null && x.Health <= SmiteRedDamage)
                                Player.Spellbook.CastSpell(SmiteSlot, x);
                        }
                    }
                }
            }
            if (Player.HasBuffOfType(BuffType.Stun) || Player.HasBuffOfType(BuffType.Taunt) || Player.HasBuffOfType(BuffType.Suppression)
                || Player.HasBuffOfType(BuffType.Knockup) || Player.HasBuffOfType(BuffType.Knockback) || Player.HasBuffOfType(BuffType.Fear))
            {
                if (Orbwalking.LastAATick + Game.Ping / 2 <= Utils.GameTimeTickCount && Orbwalking.LastAATick != 0
                    && Utils.GameTimeTickCount <= Orbwalking.LastAATick + Game.Ping + 40 + Player.AttackCastDelay * 1000)
                {
                    DelayAction.Remove(1);
                    Orbwalking.ResetAutoAttackTimer();
                }
            }
        }
        public static bool HasItem()
        {
            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady() || ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void CastItem()
        {

            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady())
                ItemData.Tiamat_Melee_Only.GetItem().Cast();
            if (ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
                ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }
        public static void checkbuff()
        {
            var temp = Player.Buffs.Aggregate("", (current, buff) => current + (buff.Name + "(" + buff.Count + ")" + ", "));
            if (temp != null)
                Game.Say(temp);
        }

        #region Smite
        private static bool hasSmite { get { return SmiteSlot != SpellSlot.Unknown; } }
        private static List <int> listsmitedamge = new List<int> {390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000};
        private static SpellSlot SmiteSlot;
        private static bool hasSmiteRed
        {
            get
            {
                return hasSmite && (new string[] {"s5_summonersmiteduel"}).Contains(Player.GetSpell(SmiteSlot).Name);
            }
        }
        private static bool hasSmiteBlue
        {
            get
            {
                return hasSmite && (new string[] { "s5_summonersmiteplayerganker" }).Contains(Player.GetSpell(SmiteSlot).Name);
            }
        }
        private static int SmiteRedDamage { get { return 54 + 6*Player.Level; } }
        private static int SmiteBlueDamage { get { return 20 + 8 * Player.Level; } }
        private static int SmiteDamage { get { return listsmitedamge[Player.Level-1]; } }
        #endregion Smite
        private static void ComboModeSwitch()
        {
            var comboMode = mode;
            var lasttime = Utils.GameTimeTickCount - _lastTick;
            if (!Menu.Item("ComboSwitch").GetValue<KeyBind>().Active ||
                lasttime <= Game.Ping)
            {
                return;
            }

            switch (comboMode)
            {
                case "Snare":
                    Menu.Item("ComboMode").SetValue(new StringList(new[] { "Snare", "Burst", "Auto", "Always Q" }, 1));
                    _lastTick = Utils.GameTimeTickCount + 300;
                    break;
                case "Burst":
                    Menu.Item("ComboMode").SetValue(new StringList(new[] { "Snare", "Burst", "Auto", "Always Q" }, 2));
                    _lastTick = Utils.GameTimeTickCount + 300;
                    break;
                case "Auto":
                    Menu.Item("ComboMode").SetValue(new StringList(new[] { "Snare", "Burst", "Auto", "Always Q" }, 3));
                    _lastTick = Utils.GameTimeTickCount + 300;
                    break;
                case "Always Q":
                    Menu.Item("ComboMode").SetValue(new StringList(new[] { "Snare", "Burst", "Auto", "Always Q" }, 0));
                    _lastTick = Utils.GameTimeTickCount + 300;
                    break;
            }
        }
        private static int _lastTick;
        private static void DrawSelectedTarget()
        {
            if (notify)
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null)
                {
                    if (notifyselected.Text == target.ChampionName)
                    {
                        return;
                    }
                    else
                    {
                        Notifications.RemoveNotification(notifyselected);
                        notifyselected = new Notification(target.ChampionName);
                        Notifications.AddNotification(notifyselected);
                    }
                }
                else
                {
                    if (notifyselected.Text == "null")
                    {
                        return;
                    }
                    else
                    {
                        Notifications.RemoveNotification(notifyselected);
                        notifyselected = new Notification("null");
                        Notifications.AddNotification(notifyselected);
                    }
                }
            }
            else
            {
                Notifications.RemoveNotification(notifyselected);
            }
        }
        private static Notification notifyselected = new Notification("null");

    }
}
