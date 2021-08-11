using System;
using Exiled.API.Features;
using Player = Exiled.Events.Handlers.Player;
using SCP106 = Exiled.Events.Handlers.Scp106;
using Server = Exiled.Events.Handlers.Server;

namespace Scp106X
{
    using System.Collections.Generic;
    using global::Scp106X.Components;

    public class Scp106X : Plugin<Config>
    {
        public Dictionary<Exiled.API.Features.Player, Scp106Component> Scp106Components = new Dictionary<Exiled.API.Features.Player, Scp106Component>(); 
        internal static Scp106X Instance { get; } = new Scp106X();
        private static Scp106X _singleton;
        private EventHandlers _handlers;
        public override string Author => "DGvagabond";
        public override string Name => "Scp106X";
        public override Version Version { get; } = new Version(1,0,0);
        public override Version RequiredExiledVersion { get; } = new Version(2,11,1);

        public override void OnEnabled()
        {
            _singleton = this;
            try
            {
                RegisterEvents();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load plugin:\n{e.StackTrace}");
            }
        }

        public override void OnDisabled()
        {
            try
            {
                UnregisterEvents();
            }
            catch (Exception e)
            {
                Log.Error($"Error during event unloading:\n{e.StackTrace}");
            }
        }

        private void RegisterEvents()
        {
            Log.Debug($"Loading events...", Config.Debug);
            _handlers = new EventHandlers(this);

            Server.WaitingForPlayers += _handlers.OnWaiting;
            
            SCP106.CreatingPortal += _handlers.OnPortalCreate;

            Player.Hurting += _handlers.OnHurt;
            Player.SyncingData += _handlers.OnSync;
            Player.ChangingRole += _handlers.OnRoleChange;
            Player.EnteringPocketDimension += _handlers.OnPocket;
            Player.EscapingPocketDimension += _handlers.OnPocketExit;
            Player.FailingEscapePocketDimension += _handlers.OnPocketFail;
        }
        private void UnregisterEvents()
        {
            Log.Debug($"Unloading events...", Config.Debug);
            Server.WaitingForPlayers -= _handlers.OnWaiting;
            
            SCP106.CreatingPortal -= _handlers.OnPortalCreate;

            Player.Hurting -= _handlers.OnHurt;
            Player.SyncingData -= _handlers.OnSync;
            Player.ChangingRole -= _handlers.OnRoleChange;
            Player.EnteringPocketDimension -= _handlers.OnPocket;
            Player.EscapingPocketDimension -= _handlers.OnPocketExit;
            Player.FailingEscapePocketDimension -= _handlers.OnPocketFail;
            
            _handlers = null;
        }
    }
}