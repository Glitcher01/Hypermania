using System.Collections.Generic;
using Design;
using Design.Animation;
using Game.Sim;
using Game.View;
using Netcode.P2P;
using Netcode.Rollback;
using Steamworks;
using UnityEngine;

namespace Game
{
    public abstract class GameRunner : MonoBehaviour
    {
        [SerializeField]
        protected GameView _view;

        [SerializeField]
        protected CharacterConfigStore _characterConfigs;

        [SerializeField]
        protected bool _drawHitboxes;

        /// <summary>
        /// The current state of the runner. If you derive from this class, it must be initialized on Init();
        /// </summary>
        protected GameState _curState;

        /// <summary>
        /// The characters of each player. _characters[i] should represent the chararcter being played by handle i. If
        /// you derive from this class, it must be initialized on Init();
        /// </summary>
        protected CharacterConfig[] _characters;

        public abstract void Init(
            List<(PlayerHandle playerHandle, PlayerKind playerKind, SteamNetworkingIdentity address)> players,
            P2PClient client
        );
        public abstract void Poll(float deltaTime);
        public abstract void Stop();

        public void OnDrawGizmos()
        {
            if (!_drawHitboxes)
                return;
            if (
                _curState == null
                || _characters == null
                || _view == null
                || _view.Fighters == null
                || _curState.Fighters == null
            )
                return;

            for (int i = 0; i < _curState.Fighters.Length; i++)
            {
                var fighterView = _view.Fighters[i];
                CharacterAnimation anim = _curState.Fighters[i].AnimState;
                int tick = _curState.Frame - _curState.Fighters[i].AnimSt;
                FrameData frame = _characters[i].GetFrameData(anim, tick);

                Transform t = fighterView.transform;

                foreach (var box in frame.Boxes)
                {
                    var kind = box.Props.Kind;
                    if (kind == HitboxKind.Hurtbox)
                        Gizmos.color = Color.blue;
                    else if (kind == HitboxKind.Hitbox)
                        Gizmos.color = Color.red;
                    else
                        continue;

                    Vector2 centerLocal = box.CenterLocal;
                    Vector2 sizeLocal = box.SizeLocal;

                    Vector3 centerWorld = t.TransformPoint(new Vector3(centerLocal.x, centerLocal.y, 0f));
                    Vector3 halfWorldX = t.TransformVector(new Vector3(sizeLocal.x * 0.5f, 0f, 0f));
                    Vector3 halfWorldY = t.TransformVector(new Vector3(0f, sizeLocal.y * 0.5f, 0f));

                    Vector3 p0 = centerWorld - halfWorldX - halfWorldY;
                    Vector3 p1 = centerWorld + halfWorldX - halfWorldY;
                    Vector3 p2 = centerWorld + halfWorldX + halfWorldY;
                    Vector3 p3 = centerWorld - halfWorldX + halfWorldY;

                    Gizmos.DrawLine(p0, p1);
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p2, p3);
                    Gizmos.DrawLine(p3, p0);
                }
            }
        }
    }
}
