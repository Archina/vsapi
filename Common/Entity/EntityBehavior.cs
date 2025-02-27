﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common.Entities
{
    /// <summary>
    /// Defines a basic entity behavior that can be attached to entities
    /// </summary>
    public abstract class EntityBehavior
    {
        public Entity entity;

        public EntityBehavior(Entity entity)
        {
            this.entity = entity;
        }

        /// <summary>
        /// Initializes the entity.
        /// </summary>
        /// <param name="properties">The properties of this entity.</param>
        /// <param name="attributes">The attributes of this entity.</param>
        public virtual void Initialize(EntityProperties properties, JsonObject attributes)
        {
            
        }

        /// <summary>
        /// The event fired when a game ticks over.
        /// </summary>
        /// <param name="deltaTime"></param>
        public virtual void OnGameTick(float deltaTime) { }

        /// <summary>
        /// The event fired when the entity is spawned (not called when loaded from the savegame).
        /// </summary>
        public virtual void OnEntitySpawn() { }

        /// <summary>
        /// The event fired when the entity is loaded from disk (not called during spawn)
        /// </summary>
        public virtual void OnEntityLoaded() { }

        /// <summary>
        /// The event fired when the entity is despawned.
        /// </summary>
        /// <param name="despawn">The reason the entity despawned.</param>
        public virtual void OnEntityDespawn(EntityDespawnReason despawn) { }

        /// <summary>
        /// The name of the property tied to this entity behavior.
        /// </summary>
        /// <returns></returns>
        public abstract string PropertyName();

        /// <summary>
        /// The event fired when the entity recieves damage.
        /// </summary>
        /// <param name="damageSource">The source of the damage</param>
        /// <param name="damage">The amount of the damage.</param>
        public virtual void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            
        }

        /// <summary>
        /// When the entity got revived (only for players and traders currently)
        /// </summary>
        public virtual void OnEntityRevive()
        {

        }

        /// <summary>
        /// The event fired when the entity falls to the ground.
        /// </summary>
        /// <param name="lastTerrainContact">the point which the entity was previously on the ground.</param>
        /// <param name="withYMotion">The vertical motion the entity had before landing on the ground.</param>
        public virtual void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
        {
        }

        /// <summary>
        /// The event fired when the entity recieves saturation.
        /// </summary>
        /// <param name="saturation">The amount of saturation recieved.</param>
        /// <param name="foodCat">The category of food recieved.</param>
        /// <param name="saturationLossDelay">The delay before the loss of saturation.</param>
        public virtual void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10, float nutritionGainMultiplier = 1f)
        {
            
        }

        /// <summary>
        /// The event fired when the server position is changed.
        /// </summary>
        /// <param name="isTeleport">Whether or not this entity was teleported.</param>
        /// <param name="handled">How this event is handled.</param>
        public virtual void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
        {
            
        }

        /// <summary>
        /// gets the drops for this specific entity.
        /// </summary>
        /// <param name="world">The world of this entity</param>
        /// <param name="pos">The block position of the entity.</param>
        /// <param name="byPlayer">The player this entity was killed by.</param>
        /// <param name="handling">How this event was handled.</param>
        /// <returns>the items dropped from this entity</returns>
        public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;

            return null;
        }

        /// <summary>
        /// The event fired when the state of the entity is changed.
        /// </summary>
        /// <param name="beforeState">The previous state.</param>
        /// <param name="handling">How this event was handled.</param>
        public virtual void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handling)
        {
            
        }

        /// <summary>
        /// The notify method bubbled up from entity.Notify()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public virtual void Notify(string key, object data)
        {
            
        }

        /// <summary>
        /// Gets the information text when highlighting this entity.
        /// </summary>
        /// <param name="infotext">The supplied stringbuilder information.</param>
        public virtual void GetInfoText(StringBuilder infotext)
        {
            
        }

        /// <summary>
        /// The event fired when the entity dies.
        /// </summary>
        /// <param name="damageSourceForDeath">The source of damage for the entity.</param>
        public virtual void OnEntityDeath(DamageSource damageSourceForDeath)
        {

        }

        /// <summary>
        /// The event fired when the entity is interacted with by the player.
        /// </summary>
        /// <param name="byEntity">The entity it was interacted with.</param>
        /// <param name="itemslot">The item slot involved (if any)</param>
        /// <param name="hitPosition">The hit position of the entity.</param>
        /// <param name="mode">The interaction mode for the entity.</param>
        /// <param name="handled">How this event is handled.</param>
        public virtual void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            
        }

        /// <summary>
        /// The event fired when the server receives a packet.
        /// </summary>
        /// <param name="player">The server player.</param>
        /// <param name="packetid">the packet id.</param>
        /// <param name="data">The data contents.</param>
        /// <param name="handled">How this event is handled.</param>
        public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            
        }

        /// <summary>
        /// The event fired when the client receives a packet.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        /// <param name="handled"></param>
        public virtual void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
        {

        }

        /// <summary>
        /// Called when a player looks at the entity with interaction help enabled
        /// </summary>
        /// <param name="world"></param>
        /// <param name="es"></param>
        /// <param name="player"></param>
        /// <param name="handled"></param>
        public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            handled = EnumHandling.PassThrough;
            return null;
        }

        public virtual void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            handled = EnumHandling.PassThrough;
        }
    }
}
