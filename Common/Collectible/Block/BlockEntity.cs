﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common
{
    /// <summary>
    /// Basic class for block entities - a data structures to hold custom information for blocks, e.g. for chests to hold it's contents
    /// </summary>
    public abstract class BlockEntity
    {
        protected List<long> TickHandlers = new List<long>();
        protected List<long> CallbackHandlers = new List<long>();

        /// <summary>
        /// The core API added to the block.  Accessable after initialization.
        /// </summary>
        public ICoreAPI Api;

        /// <summary>
        /// Position of the block for this block entity
        /// </summary>
        public BlockPos Pos;

        /// <summary>
        /// The block type at the position of the block entity. This poperty is updated by the engine if ExchangeBlock is called
        /// </summary>
        public Block Block { get; set; }

        /// <summary>
        /// List of block entity behaviors associated with this block entity
        /// </summary>
        public List<BlockEntityBehavior> Behaviors = new List<BlockEntityBehavior>();

        
        /// <summary>
        /// Creats an empty instance. Use initialize to initialize it with the api.
        /// </summary>
        public BlockEntity()
        {
        }

        public T GetBehavior<T>() where T : BlockEntityBehavior
        {
            for (int i = 0; i < Behaviors.Count; i++)
            {
                if (Behaviors[i] is T)
                {
                    return (T)Behaviors[i];
                }
            }

            return null;
        }


        /// <summary>
        /// This method is called right after the block entity was spawned or right after it was loaded from a newly loaded chunk. You do have access to the world and its blocks at this point.
        /// However if this block entity already existed then FromTreeAttributes is called first!
        /// You should still call the base method to sets the this.api field
        /// </summary>
        /// <param name="api"></param>
        public virtual void Initialize(ICoreAPI api)
        {
            this.Api = api;

            foreach (var val in Behaviors)
            {
                val.Initialize(api, val.properties);
            }
        }


        public virtual void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            this.Block = block;

            foreach (var beht in block.BlockEntityBehaviors)
            {
                if (worldForResolve.ClassRegistry.GetBlockEntityBehaviorClass(beht.Name) == null)
                {
                    worldForResolve.Logger.Warning(Lang.Get("Block entity behavior {0} for block {1} not found", beht.Name, block.Code));
                    continue;
                }

                if (beht.properties == null) beht.properties = new JsonObject(new JObject());
                BlockEntityBehavior behavior = worldForResolve.ClassRegistry.CreateBlockEntityBehavior(this, beht.Name);
                behavior.properties = beht.properties;

                Behaviors.Add(behavior);
            }
        }

        /// <summary>
        /// Registers a game tick listener that does the disposing for you when the Block is removed
        /// </summary>
        /// <param name="OnGameTick"></param>
        /// <param name="millisecondInterval"></param>
        /// <returns></returns>
        public virtual long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval)
        {
            long listenerId = Api.Event.RegisterGameTickListener(OnGameTick, millisecondInterval);
            TickHandlers.Add(listenerId);
            return listenerId;
        }

        /// <summary>
        /// Removes a registered game tick listener from the game.
        /// </summary>
        /// <param name="listenerId">the ID of the listener to unregister.</param>
        public virtual void UnregisterGameTickListener(long listenerId)
        {
            Api.Event.UnregisterGameTickListener(listenerId);
            TickHandlers.Remove(listenerId);
        }

        /// <summary>
        /// Registers a delayed callback that does the disposing for you when the Block is removed
        /// </summary>
        /// <param name="OnDelayedCallbackTick"></param>
        /// <param name="millisecondInterval"></param>
        /// <returns></returns>
        public virtual long RegisterDelayedCallback(Action<float> OnDelayedCallbackTick, int millisecondInterval)
        {
            long listenerId = Api.Event.RegisterCallback(OnDelayedCallbackTick, millisecondInterval);
            CallbackHandlers.Add(listenerId);
            return listenerId;
        }

        /// <summary>
        /// Unregisters a callback.  This is usually done automatically.
        /// </summary>
        /// <param name="listenerId">The ID of the callback listiner.</param>
        public virtual void UnregisterDelayedCallback(long listenerId)
        {
            Api.Event.UnregisterCallback(listenerId);
            CallbackHandlers.Remove(listenerId);
        }

        /// <summary>
        /// Called when the block at this position was removed in some way. Removes the game tick listeners, so still call the base method
        /// </summary>
        public virtual void OnBlockRemoved() {
            foreach (long handlerId in TickHandlers)
            {
                Api.Event.UnregisterGameTickListener(handlerId);
            }

            foreach (long handlerId in CallbackHandlers)
            {
                Api.Event.UnregisterCallback(handlerId);
            }

            foreach (var val in Behaviors)
            {
                val.OnBlockRemoved();
            }

            //api?.World.Logger.VerboseDebug("OnBlockRemoved(): {0}@{1}", this, pos);
        }

        /// <summary>
        /// Called when the block was broken in survival mode or through explosions and similar. Generally in situations where you probably want 
        /// to drop the block entity contents, if it has any
        /// </summary>
        public virtual void OnBlockBroken()
        {
            foreach (var val in Behaviors)
            {
                val.OnBlockBroken();
            }

        }

        /// <summary>
        /// Called when the chunk the block entity resides in was unloaded. Removes the game tick listeners, so still call the base method
        /// </summary>
        public virtual void OnBlockUnloaded()
        {
            if (Api != null)
            {
                foreach (long handlerId in TickHandlers)
                {
                    Api.Event.UnregisterGameTickListener(handlerId);
                }

                foreach (long handlerId in CallbackHandlers)
                {
                    Api.Event.UnregisterCallback(handlerId);
                }
            }

            foreach (var val in Behaviors)
            {
                val.OnBlockUnloaded();
            }
        }

        /// <summary>
        /// Called when the block entity just got placed, not called when it was previously placed and the chunk is loaded
        /// </summary>
        public virtual void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack?.Block != null)
            {

            }

            foreach (var val in Behaviors)
            {
                val.OnBlockPlaced();
            }
        }

        /// <summary>
        /// Called when saving the world or when sending the block entity data to the client. When overriding, make sure to still call the base method.
        /// </summary>
        /// <param name="tree"></param>
        public virtual void ToTreeAttributes(ITreeAttribute tree) {
            tree.SetInt("posx", Pos.X);
            tree.SetInt("posy", Pos.Y);
            tree.SetInt("posz", Pos.Z);
            if (Block != null)
            {
                tree.SetString("blockCode", Block.Code.ToShortString());
            }

            foreach (var val in Behaviors)
            {
                val.ToTreeAttributes(tree);
            }
        }

        /// <summary>
        /// Called when loading the world or when receiving block entity from the server. When overriding, make sure to still call the base method.
        /// FromTreeAttributes is always called before Initialize() is called, so the this.api field is not yet set!
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="worldAccessForResolve">Use this api if you need to resolve blocks/items. Not suggested for other purposes, as the residing chunk may not be loaded at this point</param>
        public virtual void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            Pos = new BlockPos(
                tree.GetInt("posx"),
                tree.GetInt("posy"),
                tree.GetInt("posz")
            );

            foreach (var val in Behaviors)
            {
                val.FromTreeAtributes(tree, worldAccessForResolve);
            }
        }

        /// <summary>
        /// Called whenever a blockentity packet at the blocks position has been received from the client
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        public virtual void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            foreach (var val in Behaviors)
            {
                val.OnReceivedClientPacket(fromPlayer, packetid, data);
            }
        }

        /// <summary>
        /// Called whenever a blockentity packet at the blocks position has been received from the server
        /// </summary>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        public virtual void OnReceivedServerPacket(int packetid, byte[] data)
        {
            foreach (var val in Behaviors)
            {
                val.OnReceivedServerPacket(packetid, data);
            }
        }


        /// <summary>
        /// When called on Server: Will resync the block entity with all its TreeAttribute to the client, but will not resend or redraw the block unless specified.
        /// When called on Client: Triggers a block changed event on the client, but will not redraw the block unless specified.
        /// </summary>
        /// <param name="redrawOnClient">When true, the block is also marked dirty and thus redrawn. When called serverside a dirty block packet is sent to the client for it to be redrawn</param>
        public void MarkDirty(bool redrawOnClient = false)
        {
            if (Api == null) return;

            Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);

            if (redrawOnClient) {
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
            }
        }

        /// <summary>
        /// Called by the block info HUD for displaying additional information
        /// </summary>
        /// <param name="forPlayer"></param>
        /// <returns></returns>
        public virtual void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            foreach (var val in Behaviors)
            {
                val.GetBlockInfo(forPlayer, dsc);
            }
        }


        /// <summary>
        /// Called by the worldedit schematic exporter so that it can also export the mappings of items/blocks stored inside blockentities
        /// </summary>
        /// <param name="blockIdMapping"></param>
        /// <param name="itemIdMapping"></param>
        public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (var val in Behaviors)
            {
                val.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
            }
        }

        /// <summary>
        /// Called by the blockschematic loader so that you may fix any blockid/itemid mappings against the mapping of the savegame, if you store any collectibles in this blockentity.
        /// Note: Some vanilla blocks resolve randomized contents in this method.
        /// Hint: Use itemstack.FixMapping() to do the job for you.
        /// </summary>
        /// <param name="oldBlockIdMapping"></param>
        /// <param name="oldItemIdMapping"></param>
        /// <param name="schematicSeed">If you need some sort of randomness consistency accross an imported schematic, you can use this value</param>
        public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
        {
            foreach (var val in Behaviors)
            {
                val.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping);
            }
        }


        /// <summary>
        /// Let's you add your own meshes to a chunk. Don't reuse the meshdata instance anywhere in your code. Return true to skip the default mesh.
        /// WARNING!
        /// The Tesselator runs in a seperate thread, so you have to make sure the fields and methods you access inside this method are thread safe.
        /// </summary>
        /// <param name="mesher">The chunk mesh, add your stuff here</param>
        /// <param name="tessThreadTesselator">If you need to tesselate something, you should use this tesselator, since using the main thread tesselator can cause race conditions and crash the game</param>
        /// <returns>True to skip default mesh, false to also add the default mesh</returns>
        public virtual bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            bool result = false;

            for (int i = 0; i < Behaviors.Count; i++)
            {
                result |= Behaviors[i].OnTesselation(mesher, tessThreadTesselator);
            }

            return result;
        }
    }

}
