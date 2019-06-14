﻿using PvPModifier.CustomWeaponAPI;
using PvPModifier.DataStorage;
using PvPModifier.Network.Packets;
using PvPModifier.Utilities;
using PvPModifier.Utilities.PvPConstants;
using PvPModifier.Variables;
using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PvPModifier.Network.Events {
    public class ProjectileEvents {
        /// <summary>
        /// Handles projectile creation.
        /// </summary>
        public static void OnNewProjectile(object sender, ProjectileNewArgs e) {
            if (!PvPModifier.Config.EnablePlugin) return;
            var projectile = Main.projectile[e.Identity];
            projectile.Initialize();
            if (projectile.active && projectile.type == e.Type) return;

            if ((TShock.Players[e.Owner]?.TPlayer?.hostile ?? false) && PvPUtils.IsModifiedProjectile(e.Type)) {
                e.Args.Handled = true;
                DbProjectile proj = Cache.Projectiles[e.Type];

                projectile.SetDefaults(proj.Shoot != -1 ? proj.Shoot : e.Type);
                projectile.velocity = e.Velocity * proj.VelocityMultiplier;
                projectile.damage = proj.Damage != -1 ? proj.Damage : e.Damage;
                projectile.active = true;
                projectile.identity = e.Identity;
                projectile.owner = e.Owner;
                projectile.position = e.Position;

                NetMessage.SendData(27, -1, -1, null, e.Identity);
            }

            e.Attacker.GetProjectileTracker().InsertProjectile(e.Identity, e.Type, e.Owner, e.Weapon.netID);
            e.Attacker.GetProjectileTracker().Projectiles[e.Type].PerformProjectileAction();
        }

        /// <summary>
        /// Runs every 1/60th second to reset any inactive projectiles.
        /// </summary>
        public static void CleanupInactiveProjectiles(EventArgs args) {
            for (int x = 0; x < Main.maxProjectiles; x++) {
                if (!Main.projectile[x].active)
                    Main.projectile[x] = new Projectile();
            }
        }

        /// <summary>
        /// Handles homing projectiles.
        /// </summary>
        public static void UpdateProjectileHoming(ProjectileAiUpdateEventArgs args) {
            if (!PvPModifier.Config.EnableHoming) return;

            var projectile = args.Projectile;

            float homingRadius = Cache.Projectiles[projectile.type].HomingRadius;
            if (homingRadius < 0) return;

            float angularVelocity = Cache.Projectiles[projectile.type].AngularVelocity;

            TSPlayer target = PvPUtils.FindClosestPlayer(projectile.position, projectile.owner, homingRadius * Constants.PixelToWorld);

            if (target != null) {
                projectile.velocity = MiscUtils.TurnTowards(projectile.velocity, projectile.position, target.TPlayer.Center, angularVelocity);
                foreach (var pvper in PvPUtils.ActivePlayers) {
                    pvper.SendRawData(new PacketWriter()
                        .SetType((short)PacketTypes.ProjectileNew)
                        .PackInt16((short)projectile.identity)
                        .PackSingle(projectile.position.X)
                        .PackSingle(projectile.position.Y)
                        .PackSingle(projectile.velocity.X)
                        .PackSingle(projectile.velocity.Y)
                        .PackSingle(projectile.knockBack)
                        .PackInt16((short)projectile.damage)
                        .PackByte((byte)projectile.owner)
                        .PackInt16((short)projectile.type)
                        .PackByte(0)
                        .PackSingle(projectile.ai[0])
                        .GetByteData());
                }
            }
        }
    }
}
