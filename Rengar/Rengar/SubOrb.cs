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
    public static class SubOrb
    {
        public static int Qtick , QcastTick, EcastTick, WcastTick , FirstBlockTick;
        public static bool FirstBlock;
        public static Obj_AI_Hero Player { get{ return ObjectManager.Player; } }
        public static void BadaoActivate()
        {
            Game.OnUpdate += Game_OnUpdate;

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
            Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (!sender.IsMe)
                return;
            if (args.Buff.Name == "rengarqbase" || args.Buff.Name == "rengarqemp")
            {
                if (Environment.TickCount - Qtick <= 1500 && Orbwalking.CanAttack())
                    Orbwalking.LastAATick = Utils.GameTimeTickCount - Game.Ping / 2 - (int)(ObjectManager.Player.AttackCastDelay * 1000);
            }
        }

        private static void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (!sender.IsMe)
                return;
            if (args.Buff.Name == "rengarqbase" || args.Buff.Name == "rengarqemp")
            {
                //Game.PrintChat(args.Buff.Name);
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;
            if (args.Slot == SpellSlot.Q)
                Qtick = Environment.TickCount;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            //if (ObjectManager.Player.HasBuff("rengarqbase") || ObjectManager.Player.HasBuff("rengarqemp"))
            //{
            //    if (Orbwalking.CanMove(90))
            //        Orbwalking.ResetAutoAttackTimer();
            //}
            if (Environment.TickCount - FirstBlockTick > 3000 && FirstBlock == true)
            {
                FirstBlock = false;
            }
        }
        private static int PlayerMana()
        {
            int mana = (int)Player.Mana;
            if (Environment.TickCount - QcastTick < 1500 + Game.Ping)
                mana++;
            if (Environment.TickCount - WcastTick < 1500 + Game.Ping)
                mana++;
            if (Environment.TickCount - EcastTick < 1500 + Game.Ping)
                mana++;
            return mana;
        }
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe)
                return;
            if (Player.Mana == 5 && FirstBlock == false && Variables.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None
                && (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot == SpellSlot.E))
            {
                args.Process = false;
                FirstBlockTick = Environment.TickCount;
                FirstBlock = true;
            }
            return;

            if (args.Slot == SpellSlot.Q)
            {
                QcastTick = Environment.TickCount;
                if (PlayerMana() > 5)
                {
                    args.Process = false;
                    QcastTick = 0;
                }
            }
            if (args.Slot == SpellSlot.W)
            {
                WcastTick = Environment.TickCount;
                if (PlayerMana() > 5)
                {
                    args.Process = false;
                    WcastTick = 0;
                }
            }
            if (args.Slot == SpellSlot.E)
            {
                EcastTick = Environment.TickCount;
                if (PlayerMana() > 5)
                {
                    args.Process = false;
                    EcastTick = 0;
                }
            }

        }
    }
}
