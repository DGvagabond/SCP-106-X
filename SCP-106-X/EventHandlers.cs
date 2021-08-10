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

            ev.Player.ReferenceHub.GetComponent<Scp106Component>().Stalk(true);
            Timing.CallDelayed(2f, FindPortal);
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
                        var scp106 = Player.Get(RoleType.Scp106).ToList();
                        foreach (var larry in scp106)
                        {
                            larry.ShowHint($"<color=yellow><b>Pocket Dimension</b></color>\nA victim has escaped!", 10f);
                        }
                    }
                    else
                    {
                        ev.IsAllowed = true;
                        ev.Player.ReferenceHub.GetComponent<RagdollManager>().SpawnRagdoll(FindExit().Position + Vector3.up * 2f,
                            Quaternion.identity, ev.Player.Rotation, 6,
                            new PlayerStats.HitInfo(9999999, ev.Player.UserId, DamageTypes.Pocket, ev.Player.Id), true,
                            ev.Player.UserId, ev.Player.Nickname, 0);
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
                ev.Player.ReferenceHub.gameObject.AddComponent<Scp106Component>();
                ev.Player.ArtificialHealthDecay = 0f;
                ev.Player.ReferenceHub.GetComponent<Scp106Component>().stalkCooldown = Time.time + 100f;
            }
            else if (ev.Player.ReferenceHub.GetComponent<Scp106Component>() != null)
            {
                var comp = ev.Player.ReferenceHub.GetComponent<Scp106Component>();
                Object.Destroy(comp);
                ev.Player.ArtificialHealthDecay = 1f;
            }
        }

        public void OnSync(SyncingDataEventArgs ev)
        {
            if (ev.Player == null) return;
            if (_portal == null) return;
            if (Player.Get(RoleType.Scp106).IsEmpty()) return;

            FindPortal();
            
            if (Vector3.Distance(ev.Player.Position, _portal.transform.position) <= 2.5 && _portal.transform.position != Vector3.zero)
                DoAnimation(ev.Player, false);
        }
        
        private void FindPortal()
        {
            if(_portal == null)
                _portal = GameObject.Find("SCP106_PORTAL");
        }

        private Room FindExit()
        {
            var rooms = Map.Rooms.ToList();
            if (Map.IsLCZDecontaminated && !Warhead.IsDetonated && !Warhead.IsInProgress)
            {
                rooms = rooms.Where(x => x.Zone != ZoneType.LightContainment).ToList();
            }
            if (Map.IsLCZDecontaminated && Warhead.IsInProgress)
            {
                rooms = rooms.Where(x => x.Zone != ZoneType.HeavyContainment && x.Zone != ZoneType.LightContainment).ToList();
            }
            if (Warhead.IsDetonated)
            {
                rooms = rooms.Where(x => x.Zone == ZoneType.Surface ).ToList();
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

            Timing.RunCoroutine(Animation(player, sinkhole));
        }

        private IEnumerator<float> Animation(Player player, bool sinkhole)
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
            if (sinkhole)
                player.EnableEffect(EffectType.SinkHole);
        }
    }
}