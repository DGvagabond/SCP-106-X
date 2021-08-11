using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using Scp106X.Components;
using UnityEngine;

namespace Scp106X
{
    public class EventHandlers
    {
        private readonly Scp106X _plugin;
        public EventHandlers(Scp106X plugin) => _plugin = plugin;
        private GameObject _portal;
        private System.Random _rand = new System.Random();
        private List<Player> _victims = new List<Player>();

        public void OnWaiting()
        {
            _portal = null;
            _plugin.Scp106Components.Clear();
        }

        public void OnHurt(HurtingEventArgs ev)
        {
            if (ev.Attacker.Role == RoleType.Scp106)
            {
                ev.IsAllowed = !ev.Target.IsInPocketDimension;
            }
        }

        public void OnPortalCreate(CreatingPortalEventArgs ev)
        {
            if (ev.Player.ReferenceHub.scp106PlayerScript.goingViaThePortal) return;
            if (ev.Player.IsInPocketDimension)
            {
                ev.Player.ShowHint($"<color=yellow><b>Portal Failure</b></color>\nYou can not create a portal in your own dimension.", 5f);
                ev.IsAllowed = false;
                return;
            }

            if (_plugin.Scp106Components.ContainsKey(ev.Player))
                _plugin.Scp106Components[ev.Player].Stalk(true);
            Timing.CallDelayed(2f, () => FindPortal(ev.Player));
        }

        public void OnPocket(EnteringPocketDimensionEventArgs ev)
        {
            if (ev.Player.Side == Side.Scp) return;
            
            ev.Scp106.ArtificialHealth += ev.Player.Health;
            // ev.Player.ShowHint($"<color=yellow><b>Pocket Dimension</b></color>\nThe longer you're here, the most powerful SCP-106 becomes. Escape quickly.", 10f);
            _victims.Add(ev.Player);
            // some AH constant gain thing here
        }
        
        public void OnPocketFail(FailingEscapePocketDimensionEventArgs ev)
        {
            switch (ev.Player.Side)
            {
                case Side.Scp:
                    ev.IsAllowed = false;
                    ev.Player.Position = Map.FindParentRoom(_portal).Position + Vector3.up * 2f;
                    break;
                default:
                    if (Player.Get(RoleType.Scp106).IsEmpty())
                    {
                        ev.IsAllowed = false;
                        ev.Player.Position = FindExit().Position + Vector3.up * 2f;
                        _victims.Remove(ev.Player);
                        foreach (var larry in Player.Get(RoleType.Scp106))
                        {
                            larry.ShowHint($"<color=yellow><b>Pocket Dimension</b></color>\nA victim has escaped!", 10f);
                        }
                    }
                    else
                    {
                        ev.IsAllowed = true;
                        Map.SpawnRagdoll(ev.Player, DamageTypes.Pocket, FindExit().Position + Vector3.up * 2f);
                        _victims.Remove(ev.Player);
                    }
                    break;
            }
        }
        
        public void OnPocketExit(EscapingPocketDimensionEventArgs ev)
        {
            if (ev.Player.Side == Side.Scp)
            {
                ev.TeleportPosition = Map.FindParentRoom(_portal).Position + Vector3.up * 2f;
            }
            else
            {
                ev.TeleportPosition = FindExit().Position + Vector3.up * 2f;
                _victims.Remove(ev.Player);
                var scp106 = Player.Get(RoleType.Scp106).ToList();
                foreach (var larry in scp106)
                {
                    larry.ShowHint($"<color=yellow><b>Pocket Dimension</b></color>\nA victim has escaped!", 10f);
                }
            }
        }

        public void OnRoleChange(ChangingRoleEventArgs ev)
        {
            if (ev.NewRole == RoleType.Scp106)
            {
                Scp106Component comp = ev.Player.GameObject.GetComponent<Scp106Component>();
                if (comp == null)
                    comp = ev.Player.ReferenceHub.gameObject.AddComponent<Scp106Component>();
                ev.Player.ArtificialHealthDecay = 0f;
                comp.stalkCooldown = Time.time + 100f;
            }
            else if (ev.Player.ReferenceHub.GetComponent<Scp106Component>() is Scp106Component comp)
            {
                Object.Destroy(comp);
                ev.Player.ArtificialHealthDecay = 1f;
            }
        }

        public void OnSync(SyncingDataEventArgs ev)
        {
            if (ev.Player == null) return;
            if (_portal == null) return;
            var larries = Player.Get(RoleType.Scp106).ToList();
            if (larries.IsEmpty()) return;

            FindPortal(larries.FirstOrDefault());
            if ((ev.Player.Position - _portal.transform.position).sqrMagnitude <= 6.25 && _portal.transform.position != Vector3.zero)
                DoAnimation(ev.Player, false);
        }
        
        private void FindPortal(Player player)
        {
            if(_portal == null)
                _portal = player.ReferenceHub.scp106PlayerScript.portalPrefab;
        }

        private Room FindExit()
        {
            var rooms = Map.Rooms.ToList();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (Map.IsLCZDecontaminated && rooms[i].Zone == ZoneType.LightContainment)
                    rooms.RemoveAt(i);
                if (Warhead.IsInProgress || Warhead.IsDetonated && (rooms[i].Zone == ZoneType.HeavyContainment || rooms[i].Zone == ZoneType.LightContainment))
                    rooms.RemoveAt(i);
            }

            var room = rooms[_rand.Next(rooms.Count)];
            
            return room;
        }
        
        private void DoAnimation(Player player, bool sinkhole)
        {
            if (player.IsGodModeEnabled || player.IsInPocketDimension) return;

            if (!sinkhole && Player.List.Any(x => x.ReferenceHub.scp106PlayerScript != null && x.ReferenceHub.scp106PlayerScript.goingViaThePortal))
                return;

            if (player.ReferenceHub.scp106PlayerScript.goingViaThePortal) return;

            player.ReferenceHub.scp106PlayerScript.goingViaThePortal = true;

            Timing.RunCoroutine(Animation(player));
        }

        private IEnumerator<float> Animation(Player player)
        {
            for (var i = 0; i < 50; i++)
            {
                var pos = player.Position;
                pos.y -= 0.05f;
                player.Position = pos;

                yield return Timing.WaitForOneFrame;
            }

            player.Position = Vector3.up * -1997;
            player.EnableEffect(EffectType.Corroding);

            yield return Timing.WaitForSeconds(0.1f);
            player.ReferenceHub.scp106PlayerScript.goingViaThePortal = false;
            player.EnableEffect(EffectType.SinkHole);
        }
    }
}