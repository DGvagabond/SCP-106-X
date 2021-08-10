using System;
using System.Linq;
using Exiled.API.Features;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scp106X.Components
{
    public class Scp106Component : MonoBehaviour
    {
        private Player _player;
        private Vector3 _oldPos = Vector3.zero;
        public bool announced = false;
        public float stalkCooldown = 0;
        public float portalCooldown = 0;

        public void Awake()
        {
            _player = Player.Get(gameObject);
        }

        public void OnDestroy()
        {
            _player = null;
        }

        public void Update()
        {
            if (_player.Role != RoleType.Scp106) return;
            if(!announced && Time.time >= stalkCooldown)
            {
                announced = true;
                _player.ShowHint($"<color=yellow><b>Stalk Available</b></color>\nStalk is ready for use", 10f);
            }
        }
        
        public bool PocketAnimation()
        {
            if (_player.ReferenceHub.scp106PlayerScript.goingViaThePortal) return false;

            if(_player.IsInPocketDimension)
            {
                var portalpos = _player.ReferenceHub.scp106PlayerScript.portalPosition;
                _player.ReferenceHub.scp106PlayerScript.portalPosition = _oldPos;
                _player.ReferenceHub.scp106PlayerScript.CallCmdUsePortal();
                MEC.Timing.CallDelayed(3.2f, () => _player.ReferenceHub.scp106PlayerScript.portalPosition = portalpos);
            }
            else
            {
                _oldPos = _player.Position;
                _oldPos.y -= 2;
                var portalpos = _player.ReferenceHub.scp106PlayerScript.portalPosition;
                _player.ReferenceHub.scp106PlayerScript.portalPosition = Vector3.up * -2000;
                _player.ReferenceHub.scp106PlayerScript.CallCmdUsePortal();
                MEC.Timing.CallDelayed(3.2f, () => _player.ReferenceHub.scp106PlayerScript.portalPosition = portalpos);
            }

            return true;
        }

        public void Stalk(bool check)
        {
            if (_player.ReferenceHub.scp106PlayerScript.goingViaThePortal) return;

            if (check)
            {
                var flag = Time.time - portalCooldown > 2;

                portalCooldown = Time.time;

                if (flag)
                    return;
            }

            if (stalkCooldown > Time.time)
            {
                _player.ShowHint($"<color=yellow><b>Stalk Cooldown</b></color>\nStalk can be used in {Math.Ceiling(stalkCooldown - Time.time)} seconds.", 10f);
                return;
            }

            var players = Player.List.Where(x => x.Team != Team.SCP && x.Role != RoleType.Spectator && x.Role != RoleType.Scp079 && x.Role != RoleType.None && x.IsInPocketDimension);

            if (!players.Any())
            {
                _player.ShowHint($"<color=yellow><b>Stalk Cooldown</b></color>\nNo target was found", 10f);
                return;
            }

            var pos = players.ElementAt(Random.Range(0, Player.List.Count())).Position;
            pos.y -= 2;

            var portalpos = _player.ReferenceHub.scp106PlayerScript.portalPosition;
            _player.ReferenceHub.scp106PlayerScript.portalPosition = pos;
            _player.ReferenceHub.scp106PlayerScript.CallCmdUsePortal();
            MEC.Timing.CallDelayed(3.5f, () => _player.ReferenceHub.scp106PlayerScript.portalPosition = portalpos);

            stalkCooldown = Time.time + Scp106X.Instance.Config.StalkCooldown;
            announced = false;
        }
    }
}