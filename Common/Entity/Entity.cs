﻿using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using System;
using System.Linq;
using Vintagestory.API.Util;
using System.Text;

namespace Vintagestory.API.Common.Entities
{
    public delegate void PhysicsTickDelegate(float accum, Vec3d prevPos);

    /// <summary>
    /// The basic class for all entities in the game
    /// </summary>
    public abstract class Entity : RegistryObject
    {
        public static WaterSplashParticles SplashParticleProps = new WaterSplashParticles();
        public static AdvancedParticleProperties[] FireParticleProps = new AdvancedParticleProperties[3];

        public static AirBubbleParticles AirBubbleParticleProps = new AirBubbleParticles();
        public static SimpleParticleProperties bioLumiParticles;
        public static NormalizedSimplexNoise bioLumiNoise;

        static Entity()
        {
            // Ember cubicles
            FireParticleProps[0] = new AdvancedParticleProperties()
            {
                HsvaColor = new NatFloat[] { NatFloat.createUniform(30, 20), NatFloat.createUniform(255, 50), NatFloat.createUniform(255, 50), NatFloat.createUniform(255, 0) },
                GravityEffect = NatFloat.createUniform(0,0),
                Velocity = new NatFloat[] { NatFloat.createUniform(0.2f, 0.05f), NatFloat.createUniform(0.5f, 0.1f), NatFloat.createUniform(0.2f, 0.05f) },
                Size = NatFloat.createUniform(0.25f, 0),
                Quantity = NatFloat.createUniform(0.25f, 0),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f),
                SelfPropelled = true

            };

            // Fire particles
            FireParticleProps[1] = new AdvancedParticleProperties()
            {
                HsvaColor = new NatFloat[] { NatFloat.createUniform(30, 20), NatFloat.createUniform(255, 50), NatFloat.createUniform(255, 50), NatFloat.createUniform(255, 0) },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16),
                GravityEffect = NatFloat.createUniform(0, 0),
                Velocity = new NatFloat[] { NatFloat.createUniform(0f, 0.02f), NatFloat.createUniform(0f, 0.02f), NatFloat.createUniform(0f, 0.02f) },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0),
                VertexFlags = 128,
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1f),
                LifeLength = NatFloat.createUniform(0.5f, 0),
                ParticleModel = EnumParticleModel.Quad
            };

            // Smoke particles
            FireParticleProps[2] = new AdvancedParticleProperties()
            {
                HsvaColor = new NatFloat[] { NatFloat.createUniform(0, 0), NatFloat.createUniform(0, 0), NatFloat.createUniform(40, 30), NatFloat.createUniform(220, 50) },
                OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16),
                GravityEffect = NatFloat.createUniform(0, 0),
                Velocity = new NatFloat[] { NatFloat.createUniform(0f, 0.05f), NatFloat.createUniform(0.2f, 0.3f), NatFloat.createUniform(0f, 0.05f) },
                Size = NatFloat.createUniform(0.3f, 0.05f),
                Quantity = NatFloat.createUniform(0.25f, 0),
                SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 1.5f),
                LifeLength = NatFloat.createUniform(1.5f, 0),
                ParticleModel = EnumParticleModel.Quad,
                SelfPropelled = true,
            };


            bioLumiParticles = new SimpleParticleProperties()
            {
                Color = ColorUtil.ToRgba(255, 0, 230, 142),
                MinSize = 0.03f,
                MaxSize = 0.1f,
                MinQuantity = 1,
                GravityEffect = 0f,
                LifeLength = 0.5f,
                ParticleModel = EnumParticleModel.Quad,
                ShouldDieInAir = true,
                VertexFlags = (byte)255,
                MinPos = new Vec3d(),
                AddPos = new Vec3d()
            };

            bioLumiParticles.ShouldDieInAir = true;
            bioLumiParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150);
            bioLumiParticles.MinSize = 0.03f;
            bioLumiParticles.MaxSize = 0.1f;

            bioLumiNoise = new NormalizedSimplexNoise(new double[] { 1, 0.5 }, new double[] { 5, 10 }, 097901);
        }

        #region Entity Fields

        /// <summary>
        /// World where the entity is spawned in. Available on the game client and server.
        /// </summary>
        public IWorldAccessor World;

        /// <summary>
        /// The api, if you need it. Available on the game client and server.
        /// </summary>
        public ICoreAPI Api;

        /// <summary>
        /// The vanilla physics systems will call this method if a physics behavior was assigned to it. The game client for example requires this to be called for the current player to properly render the player. Available on the game client and server.
        /// </summary>
        public PhysicsTickDelegate PhysicsUpdateWatcher;

        /// <summary>
        /// Server simulated animations. Only takes care of stopping animations once they're done
        /// Set and Called by the Entities ServerSystem
        /// </summary>
        public IAnimationManager AnimManager;

        /// <summary>
        /// An uptime value running activities. Available on the game client and server. Not synchronized.
        /// </summary>
        public Dictionary<string, long> ActivityTimers = new Dictionary<string, long>();
        
        
        /// <summary>
        /// Client position
        /// </summary>
        public SyncedEntityPos Pos = new SyncedEntityPos();

        /// <summary>
        /// Server simulated position. May not exactly match the client positon
        /// </summary>
        public EntityPos ServerPos = new EntityPos();

        /// <summary>
        /// Server simulated position copy. Needed by Entities server system to send pos updatess only if ServerPos differs noticably from PreviousServerPos
        /// </summary>
        public EntityPos PreviousServerPos = new EntityPos();

        /// <summary>
        /// The position where the entity last had contact with the ground. Set by the game client and server.
        /// </summary>
        public Vec3d PositionBeforeFalling = new Vec3d();        

        public long InChunkIndex3d;

        /// <summary>
        /// The entities collision box. Offseted by the animation system when necessary. Set by the game client and server.
        /// </summary>
        public Cuboidf CollisionBox;

        /// <summary>
        /// The entities collision box. Not Offseted. Set by the game client and server.
        /// </summary>
        public Cuboidf OriginCollisionBox;

        /// <summary>
        /// Used by the teleporter block
        /// </summary>
        public bool Teleporting;
        /// <summary>
        /// Used by the server to tell connected clients that the next entity position packet should not have its position change get interpolated. Gets set to false after the packet was sent
        /// </summary>
        public bool IsTeleport;


        /// <summary>
        /// A unique identifier for this entity. Set by the game client and server.
        /// </summary>
        public long EntityId;

        /// <summary>
        /// The range in blocks the entity has to be to a client to do physics and AI. When outside range, then <seealso cref="State"/> will be set to inactive
        /// </summary>
        public int SimulationRange;

        /// <summary>
        /// The face the entity is climbing on. Null if the entity is not climbing. Set by the game client and server.
        /// </summary>
        public BlockFacing ClimbingOnFace;

        /// <summary>
        /// Set by the game client and server.
        /// </summary>
        public Cuboidf ClimbingOnCollBox;

        /// <summary>
        /// True if this entity is in touch with the ground. Set by the game client and server.
        /// </summary>
        public bool OnGround;

        /// <summary>
        /// True if the bottom of the collisionbox is inside a liquid. Set by the game client and server.
        /// </summary>
        public bool FeetInLiquid;

        public bool IsOnFire
        {
            get
            {
                return WatchedAttributes.GetBool("onFire");
            } 
            set
            {
                WatchedAttributes.SetBool("onFire", value);
            }
        }
        bool resetLightHsv;

        public bool InLava;
        public long InLavaBeginTotalMs;

        public long OnFireBeginTotalMs;

        /// <summary>
        /// True if the collisionbox is 2/3rds submerged in liquid. Set by the game client and server.
        /// </summary>
        public bool Swimming;

        /// <summary>
        /// True if the entity is in touch with something solid on the vertical axis. Set by the game client and server.
        /// </summary>
        public bool CollidedVertically;

        /// <summary>
        /// True if the entity is in touch with something solid on both horizontal axes. Set by the game client and server.
        /// </summary>
        public bool CollidedHorizontally;

        /// <summary>
        /// The current entity state. Stored in WatchedAttributes in from/tobytes, so available on the client and server side
        /// </summary>
        public EnumEntityState State;

        public EntityDespawnReason DespawnReason;

        /// <summary>
        /// Permanently stored entity attributes that are sent to client everytime they have been changed
        /// </summary>
        public SyncedTreeAttribute WatchedAttributes = new SyncedTreeAttribute();

        /// <summary>
        /// If entity debug mode is on, this info will be transitted to client and displayed above the entities head
        /// </summary>
        public SyncedTreeAttribute DebugAttributes = new SyncedTreeAttribute();

        /// <summary>
        /// Permanently stored entity attributes that are only client or only server side
        /// </summary>
        public SyncedTreeAttribute Attributes = new SyncedTreeAttribute();


        /// <summary>
        /// Set by the client renderer when the entity was rendered last frame
        /// </summary>
        public bool IsRendered;

        /// <summary>
        /// Set by the client renderer when the entity shadow was rendered last frame
        /// </summary>
        public bool IsShadowRendered;

        /// <summary>
        /// Color used when the entity is being attacked
        /// </summary>
        protected int HurtColor = ColorUtil.ToRgba(255, 255, 100, 100);

        public EntityStats Stats;
        float fireDamageAccum;

        #endregion

        #region Properties

        public EntityProperties Properties { private set; get; }

        public EntitySidedProperties SidedProperties
        {
            get
            {
                if (Properties == null) return null;
                if (World.Side.IsClient()) return Properties.Client;

                return Properties.Server;
            }
        }

        /// <summary>
        /// Should return true when this entity should be interactable by a player or other entities
        /// </summary>
        public virtual bool IsInteractable
        {
            get { return true; }
        }


        /// <summary>
        /// Used for passive physics simulation, together with the MaterialDensity to check how deep in the water the entity should float
        /// </summary>
        public virtual double SwimmingOffsetY
        {
            get { return CollisionBox.Y1 + CollisionBox.Y2 * 0.66; }
        }


        /// <summary>
        /// CollidedVertically || CollidedHorizontally
        /// </summary>
        public bool Collided { get { return CollidedVertically || CollidedHorizontally; } }

        /// <summary>
        /// ServerPos on server, Pos on client
        /// </summary>
        public EntityPos SidedPos
        {
            get { return World.Side == EnumAppSide.Server ? ServerPos : Pos; }
        }

        /// <summary>
        /// The height of the eyes for the given entity.
        /// </summary>
        public virtual Vec3d LocalEyePos { get; set; } = new Vec3d();
        

        /// <summary>
        /// If gravity should applied to this entity
        /// </summary>
        public virtual bool ApplyGravity
        {
            get { return Properties.Habitat == EnumHabitat.Land || (Properties.Habitat == EnumHabitat.Sea && !Swimming); }
        }


        /// <summary>
        /// Determines on whether an entity floats on liquids or not and how strongly items get pushed by water. Water has a density of 1000.
        /// A density below 1000 means the entity floats on top of water if has a physics simulation behavior attached to it.
        /// </summary>
        public virtual float MaterialDensity
        {
            get { return 3000; }
        }

        /// <summary>
        /// If set, the entity will emit dynamic light
        /// </summary>
        public virtual byte[] LightHsv { get; set; } = null;


        /// <summary>
        /// If the entity should despawn next server tick. By default returns !Alive for non-creatures and creatures that don't have a Decay behavior
        /// </summary>
        public virtual bool ShouldDespawn
        {
            get { return !Alive; }
        }

        /// <summary>
        /// Players and whatever the player rides on will be stored seperatly
        /// </summary>
        public virtual bool StoreWithChunk { get { return true; } }

        /// <summary>
        /// Whether this entity should always stay in Active model, regardless on how far away other player are
        /// </summary>
        public virtual bool AlwaysActive { get { return false; } }

        /// <summary>
        /// True if the entity is in state active or inactive
        /// </summary>
        public virtual bool Alive
        {
            get { return alive; /* Updated every game tick. Faster than doing a dict lookup hundreds/thousands of times */ }
            set {
                WatchedAttributes.SetInt("entityDead", value ? 0 : 1); alive = value; 
            }

        }
        private bool alive=true;


        public float IdleSoundChanceModifier
        {
            get { return WatchedAttributes.GetFloat("idleSoundChanceModifier", 1); }
            set { WatchedAttributes.SetFloat("idleSoundChanceModifier", value); }
        }

        /// <summary>
        /// Used by some renderers to apply an overal color tint on the entity
        /// </summary>
        public int RenderColor
        {
            get
            {
                int val = RemainingActivityTime("invulnerable");

                return val > 0 ? ColorUtil.ColorOverlay(HurtColor, ColorUtil.WhiteArgb, 1f - val / 500f) : ColorUtil.WhiteArgb;
            }
        }

        /// <summary>
        /// A small offset used to prevent players from clipping through the blocks above ladders: relevant if the entity's collision box is sometimes adjusted by the game code
        /// </summary>
        public virtual double LadderFixDelta { get { return 0D; } }

        #endregion


        /// <summary>
        /// Creates a new instance of an entity
        /// </summary>
        public Entity()
        {
            SimulationRange = GlobalConstants.DefaultTrackingRange;
            AnimManager = new AnimationManager();
            Stats = new EntityStats(this);
            WatchedAttributes.SetAttribute("animations", new TreeAttribute());
            WatchedAttributes.SetAttribute("extraInfoText", new TreeAttribute());
        }

        /// <summary>
        /// Called when the entity got hurt. On the client side, dmgSource is null
        /// </summary>
        /// <param name="dmgSource"></param>
        /// <param name="damage"></param>
        public virtual void OnHurt(DamageSource dmgSource, float damage)
        {

        }


        /// <summary>
        /// Called when this entity got created or loaded
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="api"></param>
        /// <param name="InChunkIndex3d"></param>
        public virtual void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            this.World = api.World;
            this.Api = api;
            this.Properties = properties;
            this.Class = properties.Class;
            this.InChunkIndex3d = InChunkIndex3d;

            alive = WatchedAttributes.GetInt("entityDead", 0) == 0;
            WatchedAttributes.SetFloat("onHurt", 0);
            int onHurtCounter = WatchedAttributes.GetInt("onHurtCounter");
            WatchedAttributes.RegisterModifiedListener("onHurt", () => {
                if (WatchedAttributes.GetFloat("onHurt", 0) == 0) return;
                int newOnHurtCounter = WatchedAttributes.GetInt("onHurtCounter");
                if (newOnHurtCounter == onHurtCounter) return;

                onHurtCounter = newOnHurtCounter;
                SetActivityRunning("invulnerable", 500);

                // Gets already called on the server directly
                if (World.Side == EnumAppSide.Client)
                {
                    OnHurt(null, WatchedAttributes.GetFloat("onHurt", 0));
                }
            });

            WatchedAttributes.RegisterModifiedListener("onFire", () =>
            {
                bool onfire = IsOnFire;
                if (onfire)
                {
                    OnFireBeginTotalMs = World.ElapsedMilliseconds;
                }

                if (onfire && LightHsv == null)
                {
                    LightHsv = new byte[] { 5, 7, 10 };
                    resetLightHsv = true;
                }
                if (!onfire && resetLightHsv)
                {
                    LightHsv = null;
                }
            });

            WatchedAttributes.RegisterModifiedListener("entityDead", updateHitBox);

            if (Properties.HitBoxSize != null)
            {
                updateHitBox();
            }

            if (AlwaysActive || api.Side == EnumAppSide.Client)
            {
                State = EnumEntityState.Active;
            }
            else
            {
                State = EnumEntityState.Inactive;

                IPlayer[] players = World.AllOnlinePlayers;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].Entity == null) continue;

                    if (Pos.InRangeOf(players[i].Entity.Pos, SimulationRange * SimulationRange))
                    {
                        State = EnumEntityState.Active;
                        break;
                    }
                }
            }

            if (api.Side.IsServer())
            {
                if (properties.Client?.FirstTexture?.Alternates != null && !WatchedAttributes.HasAttribute("textureIndex"))
                {
                    WatchedAttributes.SetInt("textureIndex", World.Rand.Next(properties.Client.FirstTexture.Alternates.Length + 1));
                }                
            }

            this.Properties.Initialize(this, api);


            AnimManager = AnimationCache.InitManager(api, AnimManager, this, Properties.Client.LoadedShape, "head");
            
            if (this is EntityPlayer)
            {
                AnimManager.HeadController = new PlayerHeadController(AnimManager, this as EntityPlayer, Properties.Client.LoadedShape);
            }

            if (api.Side == EnumAppSide.Server)
            {
                AnimManager.OnServerTick(0);
            }

            LocalEyePos.Y = Properties.EyeHeight;
        }


        private void updateHitBox()
        {
            bool alive = WatchedAttributes.GetInt("entityDead", 0) == 0;

            if (alive || Properties.DeadHitBoxSize == null)
            {
                SetHitbox(Properties.HitBoxSize.X, Properties.HitBoxSize.Y);
            }
            else
            {
                SetHitbox(Properties.DeadHitBoxSize.X, Properties.DeadHitBoxSize.Y);
            }

        }


        /// <summary>
        /// Called when something tries to given an itemstack to this entity
        /// </summary>
        /// <param name="itemstack"></param>
        /// <returns></returns>
        public virtual bool TryGiveItemStack(ItemStack itemstack)
        {
            return false;
        }




        /// <summary>
        /// Is called before the entity is killed, should return what items this entity should drop. Return null or empty array for no drops.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        /// <param name="byPlayer"></param>
        /// <returns></returns>
        public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            ItemStack[] stacks = null;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                stacks = behavior.GetDrops(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventSubsequent) return stacks;
            }

            if (handled == EnumHandling.PreventDefault) return stacks;

            if (Properties.Drops == null) return null;
            List<ItemStack> todrop = new List<ItemStack>();

            float dropMul = 1;

            if (Attributes?.GetBool("isMechanical", false) != true && byPlayer?.Entity != null)
            {
                dropMul = 1 + byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
            }

            for (int i = 0; i < Properties.Drops.Length; i++)
            {
                BlockDropItemStack bdStack = Properties.Drops[i];

                float extraMul = 1f;
                if (bdStack.DropModbyStat != null && byPlayer?.Entity != null)
                {
                    // If the stat does not exist, then GetBlended returns 1 \o/
                    extraMul = byPlayer.Entity.Stats.GetBlended(bdStack.DropModbyStat);
                }

                ItemStack stack = bdStack.GetNextItemStack(dropMul * extraMul);
                if (stack == null) continue;

                todrop.Add(stack);
                if (bdStack.LastDrop) break;
            }

            return todrop.ToArray();
        }

        /// <summary>
        /// Teleports the entity to given position. Actual teleport is delayed until target chunk is loaded.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public virtual void TeleportToDouble(double x, double y, double z, API.Common.Action onTeleported = null)
        {
            Teleporting = true;

            ICoreServerAPI sapi = this.World.Api as ICoreServerAPI;
            if (sapi != null)
            {
                sapi.WorldManager.LoadChunkColumnPriority((int)ServerPos.X / World.BlockAccessor.ChunkSize, (int)ServerPos.Z / World.BlockAccessor.ChunkSize, new ChunkLoadOptions() {  OnLoaded = () =>
                    {
                        IsTeleport = true;
                        Pos.SetPos(x, y, z);
                        ServerPos.SetPos(x, y, z);
                        PositionBeforeFalling.Set(x, y, z);
                        Pos.Motion.Set(0, 0, 0);
                        onTeleported?.Invoke();
                        Teleporting = false;
                    }
                });
                
            }
        }

        /// <summary>
        /// Teleports the entity to given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public virtual void TeleportTo(int x, int y, int z)
        {
            TeleportToDouble(x, y, z);
        }

        /// <summary>
        /// Teleports the entity to given position
        /// </summary>
        /// <param name="position"></param>
        public virtual void TeleportTo(Vec3d position)
        {
            TeleportToDouble(position.X, position.Y, position.Z);
        }

        /// <summary>
        /// Teleports the entity to given position
        /// </summary>
        /// <param name="position"></param>
        public virtual void TeleportTo(BlockPos position)
        {
            TeleportToDouble(position.X, position.Y, position.Z);
        }

        /// <summary>
        /// Teleports the entity to given position
        /// </summary>
        /// <param name="position"></param>
        public virtual void TeleportTo(EntityPos position, API.Common.Action onTeleported = null)
        {
            Pos.Yaw = position.Yaw;
            Pos.Pitch = position.Pitch;
            Pos.Roll = position.Roll;
            ServerPos.Yaw = position.Yaw;
            ServerPos.Pitch = position.Pitch;
            ServerPos.Roll = position.Roll;

            TeleportToDouble(position.X, position.Y, position.Z, onTeleported);
        }



        /// <summary>
        /// Called when the entity should be receiving damage from given source
        /// </summary>
        /// <param name="damageSource"></param>
        /// <param name="damage"></param>
        public virtual bool ReceiveDamage(DamageSource damageSource, float damage)
        {
            if ((!Alive || IsActivityRunning("invulnerable")) && damageSource.Type != EnumDamageType.Heal) return false;

            if (ShouldReceiveDamage(damageSource, damage)) {
                if (damageSource.Type != EnumDamageType.Heal)
                {
                    WatchedAttributes.SetInt("onHurtCounter", WatchedAttributes.GetInt("onHurtCounter") + 1);
                    WatchedAttributes.SetFloat("onHurt", damage); // Causes the client to be notified
                    AnimManager.StartAnimation("hurt");
                }

                foreach (EntityBehavior behavior in SidedProperties.Behaviors)
                {
                    behavior.OnEntityReceiveDamage(damageSource, damage);
                }

                if (damageSource.GetSourcePosition() != null)
                {
                    Vec3d dir = (ServerPos.XYZ - damageSource.GetSourcePosition()).Normalize();
                    dir.Y = 0.1f;
                    float factor = GameMath.Clamp((1 - Properties.KnockbackResistance) / 10f, 0, 2);
                    ServerPos.Motion.Add(dir.X * factor, dir.Y * factor, dir.Z * factor);
                }

                return true;
            }

            return false;
        }


        /// <summary>
        /// Should return true if the entity can get damaged by given damageSource. Is called by ReceiveDamage.
        /// </summary>
        /// <param name="damageSource"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        public virtual bool ShouldReceiveDamage(DamageSource damageSource, float damage)
        {
            return true;
        }


        /// <summary>
        /// Called every 1/75 second
        /// </summary>
        /// <param name="dt"></param>
        public virtual void OnGameTick(float dt)
        {
            if (World.EntityDebugMode) {
                UpdateDebugAttributes();
                DebugAttributes.MarkAllDirty();
            }

            alive = WatchedAttributes.GetInt("entityDead", 0) == 0;

            if (World.FrameProfiler.Enabled)
            {
                foreach (EntityBehavior behavior in SidedProperties.Behaviors)
                {
                    behavior.OnGameTick(dt);
                    World.FrameProfiler.Mark("entity-done-bh-" + behavior.PropertyName());
                }
            } else
            {
                foreach (EntityBehavior behavior in SidedProperties.Behaviors)
                {
                    behavior.OnGameTick(dt);
                }
            }


            if (World.Side == EnumAppSide.Client && World.Rand.NextDouble() < IdleSoundChanceModifier * Properties.IdleSoundChance / 100.0 && Alive)
            {
                PlayEntitySound("idle", null, true, Properties.IdleSoundRange);
            }

            if (InLava && World.Side == EnumAppSide.Server)
            {
                Ignite();
            }


            if (IsOnFire)
            {
                if (World.BlockAccessor.GetBlock(Pos.AsBlockPos).LiquidCode == "water" || World.ElapsedMilliseconds - OnFireBeginTotalMs > 12000)
                {
                    IsOnFire = false;
                }
                else
                {

                    if (World.Side == EnumAppSide.Client)
                    {
                        int index = Math.Min(FireParticleProps.Length - 1, Api.World.Rand.Next(FireParticleProps.Length + 1));
                        AdvancedParticleProperties particles = FireParticleProps[index];
                        particles.basePos.Set(Pos.X, Pos.Y + CollisionBox.YSize / 2, Pos.Z);

                        particles.PosOffset[0].var = CollisionBox.XSize / 2;
                        particles.PosOffset[1].var = CollisionBox.YSize / 2;
                        particles.PosOffset[2].var = CollisionBox.ZSize / 2;
                        particles.Velocity[0].avg = (float)Pos.Motion.X * 10;
                        particles.Velocity[1].avg = (float)Pos.Motion.Y * 5;
                        particles.Velocity[2].avg = (float)Pos.Motion.Z * 10;
                        particles.Quantity.avg = GameMath.Sqrt(particles.PosOffset[0].var + particles.PosOffset[1].var + particles.PosOffset[2].var) * (index == 0 ? 0.5f : (index == 1 ? 3 : 1.25f));
                        Api.World.SpawnParticles(particles);
                    }
                    else
                    {
                        fireDamageAccum += dt;
                        if (fireDamageAccum > 1f)
                        {
                            ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Fire }, 0.5f);
                            fireDamageAccum = 0;
                        }
                    }

                    if (!alive && InLava && !(this is EntityPlayer))
                    {
                        float q = GameMath.Clamp(CollisionBox.XSize * CollisionBox.YSize * CollisionBox.ZSize * 150, 10, 150);
                        Api.World.SpawnParticles(
                            q,
                            ColorUtil.ColorFromRgba(20, 20, 20, 255),
                            new Vec3d(ServerPos.X + CollisionBox.X1, ServerPos.Y + CollisionBox.Y1, ServerPos.Z + CollisionBox.Z1),
                            new Vec3d(ServerPos.X + CollisionBox.X2, ServerPos.Y + CollisionBox.Y2, ServerPos.Z + CollisionBox.Z2),
                            new Vec3f(-1f, -1f, -1f),
                            new Vec3f(2f, 2f, 2f), 
                            2, 1, 1, EnumParticleModel.Cube
                        );

                        Die(EnumDespawnReason.Combusted);
                    }
                }
            }

            if (World.Side == EnumAppSide.Server)
            {
                AnimManager.OnServerTick(dt);
            }
        }   



        public void Ignite()
        {
            IsOnFire = true;
        }


        #region Events

        /// <summary>
        /// Called by EntityShapeRenderer.cs before tesselating the entity shape
        /// </summary>
        /// <param name="entityShape"></param>
        /// <param name="shapePathForLogging"></param>
        public virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging)
        {

        }


        /// <summary>
        /// Called when the entity collided vertically
        /// </summary>
        /// <param name="motionY"></param>
        public virtual void OnFallToGround(double motionY)
        {
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnFallToGround(PositionBeforeFalling, motionY);
            }
        }

        /// <summary>
        /// Called when the entity collided with something solid and <see cref="Collided"/> was false before
        /// </summary>
        public virtual void OnCollided()
        {

        }

        /// <summary>
        /// Called when the entity got in touch with a liquid
        /// </summary>
        public virtual void OnCollideWithLiquid()
        {
            if (World.Side == EnumAppSide.Server) return;

            EntityPos pos = SidedPos;
            float yDistance = (float)Math.Abs(PositionBeforeFalling.Y - pos.Y);

            double width = CollisionBox.XSize;
            double height = CollisionBox.YSize;

            double splashStrength = 2 * GameMath.Sqrt(width * height) + pos.Motion.Length() * 10;

            if (splashStrength < 0.4f || yDistance < 0.25f) return;

            //Block block = World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y - CollisionBox.Y1), (int)pos.Z);

            string[] soundsBySize = new string[] { "sounds/environment/smallsplash", "sounds/environment/mediumsplash", "sounds/environment/largesplash" };
            string sound = soundsBySize[(int)GameMath.Clamp(splashStrength / 1.6, 0, 2)];

            splashStrength = Math.Min(10, splashStrength);

            float qmod = GameMath.Sqrt(width * height);

            World.PlaySoundAt(new AssetLocation(sound), (float)pos.X, (float)pos.Y, (float)pos.Z, null);
            BlockPos blockpos = pos.AsBlockPos;
            Vec3d aboveBlockPos = new Vec3d(Pos.X, blockpos.Y + 1.02, Pos.Z);
            World.SpawnCubeParticles(blockpos, aboveBlockPos, CollisionBox.XSize, (int)(qmod * 8 * splashStrength), 0.75f);
            World.SpawnCubeParticles(blockpos, aboveBlockPos, CollisionBox.XSize, (int)(qmod * 8 * splashStrength), 0.25f);

            if (splashStrength >= 2)
            {
                SplashParticleProps.BasePos.Set(pos.X - width / 2, pos.Y - 0.75, pos.Z - width / 2);
                SplashParticleProps.AddPos.Set(width, 0.75, width);

                SplashParticleProps.AddVelocity.Set((float)GameMath.Clamp(pos.Motion.X * 30f, -10, 10), 0, (float)GameMath.Clamp(pos.Motion.Z * 30f, -10, 10));
                SplashParticleProps.QuantityMul = (float)(splashStrength - 1) * qmod;
                
                World.SpawnParticles(SplashParticleProps);
            }

            SpawnWaterMovementParticles((float)Math.Min(0.25f, splashStrength / 10f), 0, -0.5f, 0);
        }

        protected virtual void SpawnWaterMovementParticles(float quantityMul, double offx=0, double offy = 0, double offz = 0)
        {
            if (World.Side == EnumAppSide.Server) return;

            ICoreClientAPI capi = (Api as ICoreClientAPI);
            ClimateCondition climate = capi.World.Player.Entity.selfClimateCond;
            if (climate == null) return;

            float dist = Math.Max(0, (28 - climate.Temperature)/6f) + Math.Max(0, (0.8f - climate.Rainfall) * 3f);

            double noise = bioLumiNoise.Noise(SidedPos.X / 300.0, SidedPos.Z / 300.0);
            double qmul = noise * 2 - 1 - dist;


            if (qmul < 0) return;

            // Hard coded player swim hitbox thing
            if (this is EntityPlayer && Swimming)
            {
                bioLumiParticles.MinPos.Set(SidedPos.X + 2f * CollisionBox.X1, SidedPos.Y + offy + 0.5f + 1.25f * CollisionBox.Y1, SidedPos.Z + 2f * CollisionBox.Z1);
                bioLumiParticles.AddPos.Set(3f * CollisionBox.XSize, 0.5f * CollisionBox.YSize, 3f * CollisionBox.ZSize);
            }
            else
            {
                bioLumiParticles.MinPos.Set(SidedPos.X + 1.25f * CollisionBox.X1, SidedPos.Y + offy + 1.25f * CollisionBox.Y1, SidedPos.Z + 1.25f * CollisionBox.Z1);
                bioLumiParticles.AddPos.Set(1.5f * CollisionBox.XSize, 1.5f * CollisionBox.YSize, 1.5f * CollisionBox.ZSize);
            }

            bioLumiParticles.MinQuantity = Math.Min(500, 150 * quantityMul * (float)qmul);

            bioLumiParticles.MinVelocity.Set(-0.2f + 2 * (float)Pos.Motion.X, -0.2f + 2 * (float)Pos.Motion.Y, -0.2f + 2*(float)Pos.Motion.Z);
            bioLumiParticles.AddVelocity.Set(0.4f + 2 * (float)Pos.Motion.X, 0.4f + 2 * (float)Pos.Motion.Y, 0.4f + 2 * (float)Pos.Motion.Z);
            World.SpawnParticles(bioLumiParticles);
        }

        /// <summary>
        /// Called when after the got loaded from the savegame (not called during spawn)
        /// </summary>
        public virtual void OnEntityLoaded()
        {
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnEntityLoaded();
            }

            Properties.Client.Renderer?.OnEntityLoaded();
        }

        /// <summary>
        /// Called when the entity spawns (not called when loaded from the savegame).
        /// </summary>
        public virtual void OnEntitySpawn()
        {
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnEntitySpawn();
            }

            Properties.Client.Renderer?.OnEntityLoaded();
        }

        /// <summary>
        /// Called when the entity despawns
        /// </summary>
        /// <param name="despawn"></param>
        public virtual void OnEntityDespawn(EntityDespawnReason despawn)
        {
            if (SidedProperties == null) return;
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnEntityDespawn(despawn);
            }

            AnimManager.Dispose();
        }


        /// <summary>
        /// Called when the entity has left a liquid
        /// </summary>
        public virtual void OnExitedLiquid()
        {

        }

        /// <summary>
        /// Called when an entity has interacted with this entity
        /// </summary>
        /// <param name="byEntity"></param>
        /// <param name="itemslot">If being interacted with a block/item, this should be the slot the item is being held in</param>
        /// <param name="hitPosition">Relative position on the entites hitbox where the entity interacted at</param>
        /// <param name="mode">0 = attack, 1 = interact</param>
        public virtual void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode)
        {
            EnumHandling handled = EnumHandling.PassThrough;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
                if (handled == EnumHandling.PreventSubsequent) break;
            }
        }

        /// <summary>
        /// Called when a player looks at the entity with interaction help enabled
        /// </summary>
        /// <param name="world"></param>
        /// <param name="es"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player)
        {
            EnumHandling handled = EnumHandling.PassThrough;

            List<WorldInteraction> interactions = new List<WorldInteraction>();

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                WorldInteraction[] wis = behavior.GetInteractionHelp(world, es, player, ref handled);
                if (wis != null) interactions.AddRange(wis);

                if (handled == EnumHandling.PreventSubsequent) break;
            }

            return interactions.ToArray();
        }


        /// <summary>
        /// Called by client when a new server pos arrived
        /// </summary>
        public void OnReceivedServerPos(bool isTeleport)
        {
            EnumHandling handled = EnumHandling.PassThrough;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnReceivedServerPos(isTeleport, ref handled);
                if (handled == EnumHandling.PreventSubsequent) break;
            }

            if (handled == EnumHandling.PassThrough)
            {
                Pos.SetFrom(ServerPos);
            }
        }

        /// <summary>
        /// Called when on the client side something called capi.Network.SendEntityPacket()
        /// </summary>
        /// <param name="player"></param>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
        {
            EnumHandling handled = EnumHandling.PassThrough;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnReceivedClientPacket(player, packetid, data, ref handled);
                if (handled == EnumHandling.PreventSubsequent) break;
            }
        }

        /// <summary>
        /// Called when on the server side something called sapi.Network.SendEntityPacket()
        /// Packetid = 1 is used for teleporting
        /// Packetid = 2 is used for BehaviorHarvestable
        /// </summary>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        public virtual void OnReceivedServerPacket(int packetid, byte[] data)
        {
            // Teleport packet
            if (packetid == 1)
            {
                Vec3d newPos = SerializerUtil.Deserialize<Vec3d>(data);
                this.Pos.SetPos(newPos);
                this.ServerPos.SetPos(newPos);
                this.World.BlockAccessor.MarkBlockDirty(newPos.AsBlockPos);
                return;
            }

            EnumHandling handled = EnumHandling.PassThrough;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnReceivedServerPacket(packetid, data, ref handled);
                if (handled == EnumHandling.PreventSubsequent) break;
            }
        }

        public virtual void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
        {
            AnimManager.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
        }

        /// <summary>
        /// Called by BehaviorCollectEntities of nearby entities. Should return the itemstack that should be collected. If the item stack was fully picked up, BehaviorCollectEntities will kill this entity
        /// </summary>
        /// <param name="byEntity"></param>
        /// <returns></returns>
        public virtual ItemStack OnCollected(Entity byEntity)
        {
            return null;
        }

        /// <summary>
        /// Called on the server when the entity was changed from active to inactive state or vice versa
        /// </summary>
        /// <param name="beforeState"></param>
        public virtual void OnStateChanged(EnumEntityState beforeState)
        {
            EnumHandling handled = EnumHandling.PassThrough;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnStateChanged(beforeState, ref handled);
                if (handled == EnumHandling.PreventSubsequent) return;
            }
        }

        #endregion


        /// <summary>
        /// Helper method to set the CollisionBox
        /// </summary>
        /// <param name="length"></param>
        /// <param name="height"></param>
        public virtual void SetHitbox(float length, float height)
        {
            CollisionBox = new Cuboidf()
            {
                X1 = -length/2,
                Z1 = -length/2,
                X2 = length/2,
                Z2 = length/2,
                Y2 = height
            };
            OriginCollisionBox = CollisionBox.Clone();
        }

        /// <summary>
        /// Adds given behavior to the entities list of active behaviors
        /// </summary>
        /// <param name="behavior"></param>
        public virtual void AddBehavior(EntityBehavior behavior)
        {
            SidedProperties.Behaviors.Add(behavior);
        }


        /// <summary>
        /// Removes given behavior to the entities list of active behaviors. Does nothing if the behavior has already been removed
        /// </summary>
        /// <param name="behavior"></param>
        public virtual void RemoveBehavior(EntityBehavior behavior)
        {
            SidedProperties.Behaviors.Remove(behavior);
        }

        /// <summary>
        /// Returns true if the entity has given active behavior
        /// </summary>
        /// <param name="behaviorName"></param>
        /// <returns></returns>
        public virtual bool HasBehavior(string behaviorName)
        {
            for (int i = 0; i < SidedProperties.Behaviors.Count; i++)
            {
                if (SidedProperties.Behaviors[i].PropertyName().Equals(behaviorName)) return true;
            }

            return false;
        }

        public virtual bool HasBehavior<T>() where T : EntityBehavior
        {
            for (int i = 0; i < SidedProperties.Behaviors.Count; i++)
            {
                if (SidedProperties.Behaviors[i] is T) return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the behavior instance for given entity. Returns null if it doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual EntityBehavior GetBehavior(string name)
        {
            return SidedProperties.Behaviors.FirstOrDefault(bh => bh.PropertyName().Equals(name));
        }

        /// <summary>
        /// Returns the first behavior instance for given entity of given type. Returns null if it doesn't exist.
        /// </summary>
        /// <returns></returns>
        public virtual T GetBehavior<T>() where T : EntityBehavior
        {
            return (T)SidedProperties.Behaviors.FirstOrDefault(bh => bh is T);
        }
        
		
        /// <summary>
        /// Returns true if given activity is running
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual bool IsActivityRunning(string key)
        {
            long val;
            ActivityTimers.TryGetValue(key, out val);
            return val > World.ElapsedMilliseconds;
        }

        /// <summary>
        /// Returns the remaining time on an activity in milliesconds
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual int RemainingActivityTime(string key)
        {
            long val;
            ActivityTimers.TryGetValue(key, out val);
            return (int)(val - World.ElapsedMilliseconds);
        }

        /// <summary>
        /// Starts an activity for a given duration
        /// </summary>
        /// <param name="key"></param>
        /// <param name="milliseconds"></param>
        public virtual void SetActivityRunning(string key, int milliseconds)
        {
            ActivityTimers[key] = World.ElapsedMilliseconds + milliseconds;
        }


        /// <summary>
        /// Updates the DebugAttributes tree
        /// </summary>
        public virtual void UpdateDebugAttributes()
        {
            if (World.Side != EnumAppSide.Client) return;

            DebugAttributes.SetString("Entity Id", ""+EntityId);
            

            string anims = "";
            int i = 0;
            foreach (string anim in AnimManager.ActiveAnimationsByAnimCode.Keys)
            {
                if (i++ > 0) anims += ",";
                anims += anim;
            }

            DebugAttributes.SetString("Active Animations", anims.Length > 0 ? anims : "-");
        }



        /// <summary>
        /// Serializes the slots contents to be stored in the SaveGame
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="forClient">True when being used to send an entity to the client</param>
        public virtual void ToBytes(BinaryWriter writer, bool forClient)
        {
            if (!forClient)
            {
                writer.Write(GameVersion.ShortGameVersion);
            }
            
            writer.Write(EntityId);
            WatchedAttributes.SetInt("entityState", (int)State);
            WatchedAttributes.ToBytes(writer);
            ServerPos.ToBytes(writer);
            writer.Write(PositionBeforeFalling.X);
            writer.Write(PositionBeforeFalling.Y);
            writer.Write(PositionBeforeFalling.Z);

            if (Code == null)
            {
                World.Logger.Error("Entity.ToBytes(): entityType.Code is null?! Entity will probably be incorrectly saved to disk");
            }

            writer.Write(Code?.ToShortString());

            if (!forClient)
            {
                Attributes.ToBytes(writer);
            }

            TreeAttribute tree = new TreeAttribute();
            // Tyron 19.oct 2019. Don't write animations to the savegame. I think it causes that some animations start but never stop
            // if we want to save the creatures current state to disk, we would also need to save the current AI state!
            // Tyron 26 oct. Do write animations, but only the die one. 
            // Tyron 8 nov. Do write all animations if its for the client
            //if (forClient)
            {
                AnimManager?.ToAttributes(tree, forClient);
            }

            Stats.ToTreeAttributes(WatchedAttributes, forClient);

            tree.ToBytes(writer);
        }



        /// <summary>
        /// Loads the entity from a stored byte array from the SaveGame
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="fromServer"></param>
        public virtual void FromBytes(BinaryReader reader, bool fromServer)
        {
            string version = "";
            if (!fromServer)
            {
                version = reader.ReadString();
            }

            EntityId = reader.ReadInt64();
            WatchedAttributes.FromBytes(reader);

            if (!WatchedAttributes.HasAttribute("extraInfoText"))
            {
                WatchedAttributes["extraInfoText"] = new TreeAttribute();
            }

            if (GameVersion.IsLowerVersionThan(version, "1.7.0") && this is EntityPlayer)
            {
                ITreeAttribute healthTree = WatchedAttributes.GetTreeAttribute("health");
                if (healthTree != null)
                {
                    healthTree.SetFloat("basemaxhealth", 15);
                }
            }

            if (!fromServer)
            {
                State = (EnumEntityState)WatchedAttributes.GetInt("entityState", 0);
            }
            

            ServerPos.FromBytes(reader);
            Pos.SetFrom(ServerPos);
            PositionBeforeFalling.X = reader.ReadDouble();
            PositionBeforeFalling.Y = reader.ReadDouble();
            PositionBeforeFalling.Z = reader.ReadDouble();
            Code = new AssetLocation(reader.ReadString());

            if (!fromServer)
            {
                Attributes.FromBytes(reader);
            }

            // In 1.8 animation data format was changed to use a TreeAttribute. 
            if (fromServer || GameVersion.IsAtLeastVersion(version, "1.8.0-pre.1"))
            {
                TreeAttribute animTree = new TreeAttribute();
                animTree.FromBytes(reader);
                AnimManager?.FromAttributes(animTree, version);
            } else
            {
                // Should not be too bad to just ditch pre 1.8 animations
                // as the entity ai systems start new ones eventually anyway
            }


            // Upgrade to 1500 sat
            if (GameVersion.IsLowerVersionThan(version, "1.10-dev.2") && this is EntityPlayer)
            {
                ITreeAttribute hungerTree = WatchedAttributes.GetTreeAttribute("hunger");
                if (hungerTree != null)
                {
                    hungerTree.SetFloat("maxsaturation", 1500);
                }
            }

            Stats.FromTreeAttributes(WatchedAttributes);


            // Any new data loading added here should not be loaded if below version 1.8 or 
            // you might get corrupt data from old binary animation data

            
        }



        /// <summary>
        /// Revives the entity and heals for 9999.
        /// </summary>
        public virtual void Revive()
        {
            Alive = true;
            ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Revive, Type = EnumDamageType.Heal }, 9999);
            AnimManager?.StopAnimation("die");
            IsOnFire = false;

            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnEntityRevive();
            }
        }


        /// <summary>
        /// Makes the entity despawn. Entities only drop something on EnumDespawnReason.Death
        /// </summary>
        public virtual void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
        {
            if (!Alive) return;

            Alive = false;

            if (reason == EnumDespawnReason.Death)
            {
                Api.Event.TriggerEntityDeath(this, damageSourceForDeath);

                ItemStack[] drops = GetDrops(World, Pos.AsBlockPos, null);

                if (drops != null)
                {
                    for (int i = 0; i < drops.Length; i++)
                    {
                        World.SpawnItemEntity(drops[i], SidedPos.XYZ.AddCopy(0, 0.25, 0));
                    }
                }

                AnimManager.ActiveAnimationsByAnimCode.Clear();
                AnimManager.StartAnimation("die");

                if (reason == EnumDespawnReason.Death && damageSourceForDeath != null && World.Side == EnumAppSide.Server) {
                    WatchedAttributes.SetInt("deathReason", (int)damageSourceForDeath.Source);
                    WatchedAttributes.SetInt("deathDamageType", (int)damageSourceForDeath.Type);
                    Entity byEntity = damageSourceForDeath.SourceEntity;
                    if (byEntity != null)
                    {
                        WatchedAttributes.SetString("deathByEntity", "prefixandcreature-" + byEntity.Code.Path.Replace("-", ""));
                    }
                    if (byEntity is EntityPlayer)
                    {
                        WatchedAttributes.SetString("deathByPlayer", (byEntity as EntityPlayer).Player?.PlayerName);
                    }
                }
                

                foreach (EntityBehavior behavior in SidedProperties.Behaviors)
                {
                    behavior.OnEntityDeath(damageSourceForDeath);
                }
            }

            DespawnReason = new EntityDespawnReason() {
                reason = reason,
                damageSourceForDeath = damageSourceForDeath
            };
        }


        /// <summary>
        /// Assumes that it is only called on the server
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dualCallByPlayer"></param>
        /// <param name="randomizePitch"></param>
        /// <param name="range"></param>
        public virtual void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24)
        {
            AssetLocation[] locations;
            if (Properties.ResolvedSounds != null && Properties.ResolvedSounds.TryGetValue(type, out locations) && locations.Length > 0)
            {
                World.PlaySoundAt(
                    locations[World.Rand.Next(locations.Length)], 
                    (float)SidedPos.X, (float)SidedPos.Y, (float)SidedPos.Z, 
                    dualCallByPlayer, 
                    randomizePitch, 
                    range
                );
            }
        }


        /// <summary>
        /// Should return true if this item can be picked up as an itemstack
        /// </summary>
        /// <param name="byEntity"></param>
        /// <returns></returns>
        public virtual bool CanCollect(Entity byEntity)
        {
            return false;
        }



        /// <summary>
        /// This method pings the Notify() method of all behaviors and ai tasks. Can be used to spread information to other creatures.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public virtual void Notify(string key, object data)
        {
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.Notify(key, data);
            }
        }

        /// <summary>
        /// True if given emotion state is currently set
        /// </summary>
        /// <param name="statecode"></param>
        /// <returns></returns>
        public virtual bool HasEmotionState(string statecode)
        {
            ITreeAttribute attr = Attributes.GetTreeAttribute("emotionstates");
            return attr != null && attr.HasAttribute(statecode);
        }


        /// <summary>
        /// This method is called by the BlockSchematic class a moment before a schematic containing this entity is getting exported.
        /// Since a schematic can be placed anywhere in the world, this method has to make sure the entities position is set to a value
        /// relative of the schematic origin point defined by startPos
        /// Right after calling this method, the world edit system will call .ToBytes() to serialize the entity
        /// </summary>
        /// <param name="startPos"></param>
        public virtual void WillExport(BlockPos startPos)
        {
            ServerPos.X -= startPos.X;
            ServerPos.Y -= startPos.Y;
            ServerPos.Z -= startPos.Z;

            Pos.X -= startPos.X;
            Pos.Y -= startPos.Y;
            Pos.Z -= startPos.Z;

            PositionBeforeFalling.X -= startPos.X;
            PositionBeforeFalling.Y -= startPos.Y;
            PositionBeforeFalling.Z -= startPos.Z;
        }

        /// <summary>
        /// This method is called by the BlockSchematic class a moment after a schematic containing this entity has been exported. 
        /// Since a schematic can be placed anywhere in the world, this method has to make sure the entities position is set to the correct 
        /// position in relation to the target position of the schematic to be imported.
        /// </summary>
        /// <param name="startPos"></param>
        public virtual void DidImportOrExport(BlockPos startPos)
        {
            ServerPos.X += startPos.X;
            ServerPos.Y += startPos.Y;
            ServerPos.Z += startPos.Z;

            Pos.X += startPos.X;
            Pos.Y += startPos.Y;
            Pos.Z += startPos.Z;

            PositionBeforeFalling.X += startPos.X;
            PositionBeforeFalling.Y += startPos.Y;
            PositionBeforeFalling.Z += startPos.Z;
        }

        /// <summary>
        /// Gets the name for this entity
        /// </summary>
        /// <returns></returns>
        public virtual string GetName()
        {
            if (!Alive)
            {
                return Lang.GetMatching(Code.Domain + ":item-dead-creature-" + Code.Path);
            }

            return Lang.GetMatching(Code.Domain + ":item-creature-" + Code.Path);
        }

        /// <summary>
        /// gets the info text for the entity.
        /// </summary>
        /// <returns></returns>
        public virtual string GetInfoText()
        {
            StringBuilder infotext = new StringBuilder();
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.GetInfoText(infotext);
            }

            int generation = WatchedAttributes.GetInt("generation", 0);
            
            if (generation > 0)
            {
                infotext.AppendLine(Lang.Get("Generation: {0}", generation));
            }

            if (!Alive)
            {
                if (WatchedAttributes.HasAttribute("deathByPlayer")) {
                    infotext.AppendLine(Lang.Get("Killed by Player: {0}", WatchedAttributes.GetString("deathByPlayer")));
                }
            }

            if (World.Side == EnumAppSide.Client && (World as IClientWorldAccessor).Player?.WorldData?.CurrentGameMode == EnumGameMode.Creative)
            {
                var healthTree = WatchedAttributes.GetTreeAttribute("health") as ITreeAttribute;
                if (healthTree != null) infotext.AppendLine(Lang.Get("Health: {0}/{1}", healthTree.GetFloat("currenthealth"), healthTree.GetFloat("maxhealth")));
            }

            if (WatchedAttributes.HasAttribute("extraInfoText"))
            {
                ITreeAttribute tree = WatchedAttributes.GetTreeAttribute("extraInfoText");
                foreach (var val in tree)
                {
                    infotext.AppendLine(val.Value.ToString());
                }
            }

            return infotext.ToString();
        }

        /// <summary>
        /// Starts the animation for the entity.
        /// </summary>
        /// <param name="code"></param>
        public virtual void StartAnimation(string code)
        {
            AnimManager.StartAnimation(code);
        }

        /// <summary>
        /// stops the animation for the entity.
        /// </summary>
        /// <param name="code"></param>
        public virtual void StopAnimation(string code)
        {
            AnimManager.StopAnimation(code);
        }

    }
}
