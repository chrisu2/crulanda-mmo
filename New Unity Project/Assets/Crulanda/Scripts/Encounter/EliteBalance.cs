using System;
using System.Collections.Generic;
using Crulanda.Combat;

namespace Crulanda.Encounter
{
    /// <summary>
    /// A paper fight for tuning mobs and elites (playtest note 2): one player against one or more mobs, stepped a tenth of a
    /// second at a time, with the class kits' base abilities (no talents), the game's own damage rule (CombatMath) and Mira's
    /// two spells. It is a ruler, not the game: threat, movement and the navmesh are left out, every mob stays on the player,
    /// and a Druid stays in one form. An elite's call is in it: a mob marked <see cref="Mob.called"/> stands out of the fight
    /// until the elite calls, and then comes. EliteBalanceTests holds the numbers it must give.
    /// </summary>
    public static class EliteBalance
    {
        public enum Kit { Warrior, Barkhide, Thornclaw }

        /// <summary>
        /// The kit numbers the paper fight uses. The defaults are EncounterContent/Encounter.asset's (2026-10-01); the test checks
        /// them against the asset, so a retuned ability fails there instead of quietly leaving this table behind.
        /// </summary>
        public sealed class Numbers
        {
            public float playerSwing = 2.6f, enemySwing = 2.6f, globalCooldown = 1.5f;
            public int strikePower = 12, strikeCost = 15; public float strikeCooldown = 5;
            public int guardCost = 20; public float guardCooldown = 12, guardSeconds = 5, guardMultiplier = .4f;
            public int challengeCost = 10; public float challengeCooldown = 8;
            public int vigorMax = 100, vigorRegen = 5;
            public int boughPower = 10; public float boughCooldown = 4.5f;
            public int rakePower = 6, tearPower = 10;
            public int healPower = 42, healCost = 18; public float healPerLevel = HealerCompanion.HealPerLevel; public float healCooldown = 3.5f, healCast = 1.5f;
            public int boltPower = 7; public float boltCooldown = 4;
            public int miraMana = 120;
            /// <summary>The same numbers read from the game's content (abilities by id, Guard's status, Mira's two spells, the Warrior's Vigor).</summary>
            public static Numbers From(EncounterContent c)
            {
                var n = new Numbers(); if (c == null) return n;
                Crulanda.Abilities.AbilityDefinition A(string id) { return Array.Find(c.abilities, a => a != null && a.id == id); }
                n.playerSwing = c.playerSwingInterval; n.enemySwing = c.enemySwingInterval;
                var strike = A("ability.strike"); if (strike != null) { n.strikePower = strike.power; n.strikeCost = strike.cost; n.strikeCooldown = strike.cooldown; n.globalCooldown = strike.globalCooldown; }
                var guard = A("ability.guard"); if (guard != null) { n.guardCost = guard.cost; n.guardCooldown = guard.cooldown; }
                var status = c.FindStatus("status.guard"); if (status != null) { n.guardSeconds = status.duration; n.guardMultiplier = status.incomingDamageMultiplier; }
                var challenge = A("ability.challenge"); if (challenge != null) { n.challengeCost = challenge.cost; n.challengeCooldown = challenge.cooldown; }
                if (c.playerClass != null) { n.vigorMax = c.playerClass.maxResource; n.vigorRegen = c.playerClass.combatRegen; }
                var bough = A("druid.bough_strike"); if (bough != null) { n.boughPower = bough.power; n.boughCooldown = bough.cooldown; }
                var rake = A("druid.rake"); if (rake != null) n.rakePower = rake.power;
                var tear = A("druid.tear"); if (tear != null) n.tearPower = tear.power;
                if (c.healingAbility != null) { n.healPower = c.healingAbility.power; n.healCost = c.healingAbility.cost; n.healCooldown = c.healingAbility.cooldown; n.healCast = c.healingAbility.castTime; }
                if (c.boltAbility != null) { n.boltPower = c.boltAbility.power; n.boltCooldown = c.boltAbility.cooldown; }
                return n;
            }
        }

        /// <summary>The player on paper: a class kit at a level, with what its gear gives.</summary>
        public sealed class Fighter
        {
            public Kit kit; public int level, health, armor, attackPower;
        }
        /// <summary>A mob on paper: its health and swing, and its move when it is an elite.</summary>
        public sealed class Mob
        {
            public int level, health; public float hit; public EliteMove move;
            /// <summary>
            /// It answers the elite's call: out of the fight until the elite is at its move's callAt, then the beat
            /// (SocialAggro.CallBeat) and <see cref="walk"/> seconds on the way. With no elite in the fight it is there from the start.
            /// </summary>
            public bool called; public float walk;
        }
        public struct Result
        {
            /// <summary>Every mob died and the player did not.</summary>
            public bool won;
            /// <summary>Seconds until the last mob (or the player) fell.</summary>
            public float seconds;
            /// <summary>Health the player lost, healing not taken off.</summary>
            public int taken;
            /// <summary>What was left at the end, as a share of the whole: the player's health, and the mobs' health together.</summary>
            public float playerLeft, mobsLeft;
        }

        /// <summary>
        /// A player in on-curve gear: one generated uncommon piece in every slot at the level, the suffix stats averaged over
        /// sixty seeds a slot. <paramref name="rules"/> is the class's stat rules (ClassDefinition.stats).
        /// </summary>
        public static Fighter Geared(Kit kit, DerivedStatRules rules, int level)
        {
            var db = new ItemDatabase(); const int seeds = 60;
            float armor = 0, stamina = 0, strength = 0, agility = 0, weapon = 0;
            foreach (var slot in ItemDatabase.SlotIds)
                for (int seed = 0; seed < seeds; seed++)
                {
                    var d = db.Get(ItemDatabase.GearId(slot, level, 2, seed));
                    armor += d.armor; stamina += d.stamina; strength += d.strength; agility += d.agility; weapon += d.weaponDamage;
                }
            return Make(kit, rules, level, armor / seeds, stamina / seeds, strength / seeds, agility / seeds, (int)Math.Round(weapon / seeds));
        }
        /// <summary>A player in nothing but the class's own numbers and a weapon bonus (the starting blade's 7, say).</summary>
        public static Fighter Bare(Kit kit, DerivedStatRules rules, int level, int weapon) { return Make(kit, rules, level, 0, 0, 0, 0, weapon); }
        static Fighter Make(Kit kit, DerivedStatRules rules, int level, float gearArmor, float stamina, float strength, float agility, int weapon)
        {
            var v = DerivedStatCalculator.Calculate(rules, level, stamina, strength, 0, agility, weapon);
            var f = new Fighter { kit = kit, level = level, health = v.health, armor = v.armor + (int)Math.Round(gearArmor), attackPower = v.attackPower };
            // The Druid's forms (DruidKit.ApplyStats): Barkhide 30 armour and a fifth more health, Thornclaw 6 attack power.
            if (kit == Kit.Barkhide) { f.armor += 30; f.health = (int)Math.Round(f.health * 1.2f); }
            if (kit == Kit.Thornclaw) f.attackPower += 6;
            return f;
        }
        /// <summary>A camp mob of a level on paper: a normal one, or the elite a move belongs to (its tier's multipliers on).</summary>
        public static Mob CampMob(int level, bool beast, EliteMove move = null)
        {
            bool elite = move != null;
            return new Mob {
                level = level, health = (int)Math.Round(EncounterEnemy.MobHealth(level, false, elite, beast) * (elite ? move.health : 1)),
                hit = EncounterEnemy.MobHit(level, false, elite) * (elite ? move.hit : 1), move = move };
        }
        /// <summary>A normal camp mob that answers the elite's call from <paramref name="metres"/> away at <paramref name="speed"/> metres a second.</summary>
        public static Mob Answerer(int level, bool beast, float metres, float speed)
        {
            var m = CampMob(level, beast); m.called = true; m.walk = metres / Math.Max(.1f, speed); return m;
        }

        /// <summary>
        /// Fights it out. <paramref name="careful"/>: the player answers every heavy blow (a Warrior raises Guard when it is
        /// ready and steps out when it is not; a Druid steps out), which costs the time out of reach. Careless: stands in all of
        /// it and never guards. <paramref name="mira"/>: she heals under 78% and throws her bolt above 80%, and a Warrior pays
        /// for Challenge to keep the mobs off her. The player kills the mobs in order (of those that are there); all of them
        /// swing from the start, but for those that answer the elite's call (<see cref="Mob.called"/>), who swing once they arrive.
        /// </summary>
        public static Result Fight(Fighter f, IList<Mob> mobs, bool mira, bool careful, Numbers n = null, float limit = 600)
        {
            n = n ?? new Numbers(); const float dt = .1f;
            int count = mobs.Count; var hp = new float[count]; var over = new float[count]; var tough = new float[count]; var nextSwing = new float[count]; var nextBlow = new float[count]; var release = new float[count];
            float total = 0;
            for (int i = 0; i < count; i++) { hp[i] = mobs[i].health; total += hp[i]; nextBlow[i] = mobs[i].move != null ? mobs[i].move.first : float.MaxValue; release[i] = -1;
                over[i] = mobs[i].move != null ? EncounterEnemy.OvermatchHit(mobs[i].level, f.level) : 1; tough[i] = mobs[i].move != null ? EncounterEnemy.OvermatchTaken(mobs[i].level, f.level) : 1; }
            float health = f.health; int taken = 0;
            float vigor = n.vigorMax, gcd = 0, strikeAt = 0, guardAt = 0, guardUntil = -1, challengeAt = 0, boughAt = 0, barkmendAt = 0;
            float swing = f.kit == Kit.Thornclaw ? n.playerSwing * .8f : n.playerSwing, nextAuto = swing, awayFrom = -1, awayUntil = -1, bleedAt = -1; int bleedTicks = 0;
            float bark = 0; int fang = 0;
            float mana = n.miraMana, healAt = 0, healLands = -1, boltAt = 0;
            // The elite's call: -1 until an elite in the fight is at its callAt (0, the start, when there is no elite to call).
            float calledAt = 0; for (int i = 0; i < count; i++) if (mobs[i].move != null && !string.IsNullOrEmpty(mobs[i].move.call)) calledAt = -1;
            bool Here(int i, float now) { return !mobs[i].called || (calledAt >= 0 && now >= calledAt + (calledAt > 0 ? SocialAggro.CallBeat + mobs[i].walk : 0)); }
            for (float t = 0; t < limit; t += dt)
            {
                // The first in the order who is alive and there; those still on their way are waited for.
                int target = -1; bool any = false;
                for (int i = 0; i < count; i++) { if (hp[i] <= 0) continue; any = true; if (target < 0 && Here(i, t)) target = i; }
                if (!any) return End(true, t, taken, health, f, hp, total);
                bool away = (t >= awayFrom && t < awayUntil) || target < 0;
                // ---------- the player ----------
                vigor = Math.Min(n.vigorMax, vigor + n.vigorRegen * dt);
                float dealt = 0;
                if (!away)
                {
                    if (t >= nextAuto) { nextAuto = t + swing; dealt += f.attackPower; }
                    if (f.kit == Kit.Warrior)
                    {
                        if (t >= strikeAt && t >= gcd && vigor >= n.strikeCost) { vigor -= n.strikeCost; strikeAt = t + n.strikeCooldown; gcd = t + n.globalCooldown; dealt += n.strikePower + f.attackPower; }
                        else if (mira && t >= challengeAt && t >= gcd && vigor >= n.challengeCost + n.strikeCost) { vigor -= n.challengeCost; challengeAt = t + n.challengeCooldown; gcd = t + n.globalCooldown; }
                    }
                    else if (f.kit == Kit.Barkhide)
                    {
                        if (f.level >= 4 && bark >= 30 && t >= barkmendAt && t >= gcd && health < f.health * .85f) { bark -= 30; barkmendAt = t + 8; gcd = t + n.globalCooldown; health = Math.Min(f.health, health + (float)Math.Round(f.health * .12f)); }
                        else if (t >= boughAt && t >= gcd) { boughAt = t + n.boughCooldown; gcd = t + n.globalCooldown; bark = Math.Min(100, bark + 12); dealt += (float)Math.Round(n.boughPower + f.attackPower * .5f); }
                    }
                    else if (t >= gcd)
                    {
                        gcd = t + n.globalCooldown;
                        if (fang >= 5) { fang = 0; dealt += (float)Math.Round(n.tearPower * 5 + f.attackPower * .5f); }
                        else { fang++; dealt += (float)Math.Round(n.rakePower + f.attackPower * .6f); bleedAt = t + 2; bleedTicks = 3; }
                    }
                }
                // Rake's bleed ticks on whether or not the Druid is in reach (3 + level / 2, every 2 s, three times; a new Rake starts it over).
                if (bleedTicks > 0 && t >= bleedAt) { bleedAt = t + 2; bleedTicks--; dealt += (float)Math.Round(3 + f.level / 2f); }
                // ---------- Mira ----------
                if (mira)
                {
                    mana = Math.Min(n.miraMana, mana + dt);
                    if (healLands >= 0 && t >= healLands) { healLands = -1; health = Math.Min(f.health, health + HealerCompanion.HealFor(n.healPower, f.level, n.healPerLevel)); }
                    if (healLands < 0 && t >= healAt && mana >= n.healCost && health < f.health * .78f) { mana -= n.healCost; healAt = t + n.healCooldown; healLands = t + n.healCast; }
                    else if (healLands < 0 && t >= boltAt && health > f.health * .8f) { boltAt = t + n.boltCooldown; dealt += n.boltPower; }
                }
                if (target >= 0) hp[target] -= dealt * tough[target];
                if (calledAt < 0) for (int i = 0; i < count; i++) if (mobs[i].move != null && hp[i] <= mobs[i].health * mobs[i].move.callAt) calledAt = t;
                // ---------- the mobs ----------
                for (int i = 0; i < count; i++)
                {
                    if (hp[i] <= 0 || !Here(i, t)) continue;
                    var m = mobs[i].move;
                    float interval = n.enemySwing * (m != null && hp[i] <= mobs[i].health * m.enrageAt ? m.enrageHaste : 1);
                    if (release[i] >= 0)
                    {
                        if (t < release[i]) continue;
                        release[i] = -1; nextSwing[i] = t + interval; nextBlow[i] = t + m.every;
                        if (!away) health -= Hurt(f, mobs[i].hit * over[i] * m.blow, t < guardUntil ? n.guardMultiplier : 1, ref taken, ref bark);
                    }
                    else if (t >= nextSwing[i])
                    {
                        if (m != null && t >= nextBlow[i])
                        {
                            release[i] = t + m.windup;
                            if (!careful) continue;
                            if (f.kit == Kit.Warrior && t >= guardAt && vigor >= n.guardCost) { vigor -= n.guardCost; guardAt = t + n.guardCooldown; guardUntil = t + n.guardSeconds; }
                            else { awayFrom = t + .4f; awayUntil = release[i] + .6f; }   // out of the mark and back: no blows either way
                        }
                        else if (!away) { nextSwing[i] = t + interval; health -= Hurt(f, mobs[i].hit * over[i], t < guardUntil ? n.guardMultiplier : 1, ref taken, ref bark); }
                    }
                }
                if (health <= 0) return End(false, t, taken, 0, f, hp, total);
            }
            return End(false, limit, taken, health, f, hp, total);
        }
        static float Hurt(Fighter f, float raw, float multiplier, ref int taken, ref float bark)
        {
            int d = CombatMath.Damage((int)Math.Round(raw), f.armor, multiplier); taken += d;
            if (f.kit == Kit.Barkhide) bark = Math.Min(100, bark + 5);
            return d;
        }
        /// <summary>A result as a table cell: "wins 43 s, 80% left, took 405" or "dies 31 s, mob 31% left, took 432".</summary>
        public static string Cell(Result r)
        {
            return (r.won ? "wins " : "dies ") + r.seconds.ToString("0") + " s, " + (r.won ? (r.playerLeft * 100).ToString("0") + "% left" : "mob " + (r.mobsLeft * 100).ToString("0") + "% left") + ", took " + r.taken;
        }
        static Result End(bool won, float t, int taken, float health, Fighter f, float[] hp, float total)
        {
            float left = 0; foreach (var h in hp) left += Math.Max(0, h);
            return new Result { won = won, seconds = t, taken = taken, playerLeft = Math.Max(0, health) / f.health, mobsLeft = left / total };
        }
    }
}
