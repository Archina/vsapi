﻿using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common
{
    public delegate IMountable GetMountableDelegate(IWorldAccessor world, TreeAttribute tree);

    public interface ICoreAPICommon
    {

        #region Register game content

        /// <summary>
        /// Registers a new color map. Typically used to color in-game blocks with a texture - i.e. climate and seasonal coloring
        /// </summary>
        void RegisterColorMap(ColorMap map);

        /// <summary>
        /// Registers a non-block entity. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="entity"></param>
        void RegisterEntity(string className, Type entity);

        /// <summary>
        /// Registers a non-block entity behavior. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="entityBehavior"></param>
        void RegisterEntityBehaviorClass(string className, Type entityBehavior);

        /// <summary>
        /// Register a new Blockclass. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name = "className">Class to register</param>
        /// <param name = "blockType">Name of the class</param>
        void RegisterBlockClass(string className, Type blockType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="type"></param>
        void RegisterCropBehavior(string className, Type type);


        /// <summary>
        /// Register a new BlockEntity Class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="blockentityType"></param>
        void RegisterBlockEntityClass(string className, Type blockentityType);

        /// <summary>
        /// Register a new Item Class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="itemType"></param>
        void RegisterItemClass(string className, Type itemType);

        /// <summary>
        /// Register a new block behavior class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="blockBehaviorType"></param>
        void RegisterBlockBehaviorClass(string className, Type blockBehaviorType);

        /// <summary>
        /// Register a new block entity behavior class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
        /// </summary>
        /// <param name="className"></param>
        /// <param name="blockEntityBehaviorType"></param>
        void RegisterBlockEntityBehaviorClass(string className, Type blockEntityBehaviorType);


        /// <summary>
        /// Register a new block behavior class. Must happen before any blocks are loaded. Be sure to register it on the client and server side.
        /// Make your your delegate also set tree.SetString("className", "[your className]");
        /// </summary>
        /// <param name="className"></param>
        /// <param name="blockBehaviorType"></param>
        void RegisterMountable(string className, GetMountableDelegate mountableInstancer);


        #endregion


        /// <summary>
        /// Can be used to store non-persistent, game wide data. E.g. used for firewood piles to pregenerate all meshes only once during startup
        /// </summary>
        Dictionary<string, object> ObjectCache { get; }

        /// <summary>
        /// Returns the root path of the games data folder
        /// </summary>
        /// <value></value>
        string DataBasePath { get; }

        /// <summary>
        /// Returns the path to given foldername inside the games data folder. Ensures that the folder exists
        /// </summary>
        /// <param name="foldername"></param>
        /// <returns></returns>
        string GetOrCreateDataPath(string foldername);


        /// <summary>
        /// Milo kept asking for a standardized way to load and store mod configuration data, so here you go :P
        /// For T just make a class with all fields public - this is your configuration class. Be sure to set useful default values for your settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonSerializeableData"></param>
        /// <param name="filename"></param>
        void StoreModConfig<T>(T jsonSerializeableData, string filename);

        /// <summary>
        /// Milo kept asking for a standardized way to load and store mod configuration data, so here you go :P
        /// Recommendation: Surround this call with a try/catch in case the user made a typo while changing the configuration
        /// Returns null if the file does not exist
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        T LoadModConfig<T>(string filename);


    }

    /// <summary>
    /// Common API Components that are available on the server and the client. Cast to ICoreServerAPI or ICoreClientAPI to access side specific features.
    /// </summary>
    public interface ICoreAPI : ICoreAPICommon
    {
        /// <summary>
        /// The local Logger instance.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// The command line arguments that were used to start the client or server application
        /// </summary>
        string[] CmdlArguments { get; }

        /// <summary>
        /// Returns if you are currently on server or on client
        /// </summary>
        EnumAppSide Side { get; }
        
        /// <summary>
        /// Api component to register/trigger events
        /// </summary>
        IEventAPI Event { get; }

        /// <summary>
        /// Second API Component for access/modify everything game world related
        /// </summary>
        IWorldAccessor World { get; }

        /// <summary>
        /// API Compoment for creating instances of certain classes, such as Itemstacks
        /// </summary>
        IClassRegistryAPI ClassRegistry { get; }

        /// <summary>
        /// API for sending/receiving network packets
        /// </summary>
        INetworkAPI Network { get; }


        /// <summary>
        /// API Component for loading and reloading one or multiple assets at once from the assets folder
        /// </summary>
        IAssetManager Assets { get; }

        /// <summary>
        /// API Component for checking for and interacting with other mods and mod systems
        /// </summary>
        IModLoader ModLoader { get; }


        /// <summary>
        /// Registers a new entity config for given entity class
        /// </summary>
        /// <param name="entityClassName"></param>
        /// <param name="config"></param>
        void RegisterEntityClass(string entityClassName, EntityProperties config);

    }
}
