using System.Collections;
using System.Collections.Generic;
using Design.Animation;
using UnityEngine;

namespace Game.Sim
{
    public class Physics
    {
        public class PhysicsCollision
        {
            public int BodyA;
            public int BodyB;
            public float OverlapX;
        }

        private struct BoxEntry
        {
            public int Body;
            public Vector2 BoxPos;
            public Vector2 BoxSize;
            public BoxProps BoxProps;
        }

        private readonly Pool<BoxEntry> _boxPool;
        private readonly List<int> _boxInds;
        private readonly Dictionary<ulong, PhysicsCollision> _collisions = new Dictionary<ulong, PhysicsCollision>(64);

        public Physics(int maxHitboxes)
        {
            _boxPool = new Pool<BoxEntry>(maxHitboxes);
            _boxInds = new List<int>(maxHitboxes);
        }

        public void AddBox(int body, Vector2 boxPos, Vector2 boxSize, BoxProps boxProps)
        {
            int ind = _boxPool.Spawn(
                new BoxEntry
                {
                    Body = body,
                    BoxPos = boxPos,
                    BoxSize = boxSize,
                    BoxProps = boxProps,
                }
            );
            _boxInds.Add(ind);
        }

        public void Clear()
        {
            _boxPool.Clear();
            _collisions.Clear();
            _boxInds.Clear();
        }

        private static bool AabbOverlap(in BoxData a, in BoxData b, out float overlapX)
        {
            Vector2 ah = a.SizeLocal * 0.5f;
            Vector2 bh = b.SizeLocal * 0.5f;

            float aMinX = a.CenterLocal.x - ah.x;
            float aMaxX = a.CenterLocal.x + ah.x;
            float aMinY = a.CenterLocal.y - ah.y;
            float aMaxY = a.CenterLocal.y + ah.y;

            float bMinX = b.CenterLocal.x - bh.x;
            float bMaxX = b.CenterLocal.x + bh.x;
            float bMinY = b.CenterLocal.y - bh.y;
            float bMaxY = b.CenterLocal.y + bh.y;

            float ox = Mathf.Min(aMaxX, bMaxX) - Mathf.Max(aMinX, bMinX);
            if (ox <= 0f)
            {
                overlapX = 0f;
                return false;
            }

            float oy = Mathf.Min(aMaxY, bMaxY) - Mathf.Max(aMinY, bMinY);
            if (oy <= 0f)
            {
                overlapX = 0f;
                return false;
            }

            overlapX = ox;
            return true;
        }

        private static ulong PackPair(int a, int b)
        {
            unchecked
            {
                return ((ulong)(uint)a << 32) | (uint)b;
            }
        }
    }
}
