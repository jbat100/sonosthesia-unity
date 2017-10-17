using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{

    public interface IContactVertex
    {
        int? index { get; set; } 
        Color? color { get; set; }
        Vector3? normal { get; set; }
        Vector2? uv1 { get; set; }
        Vector2? uv2 { get; set; }
        Vector2? uv3 { get; set; }
        Vector2? uv4 { get; set; }
    }

    public struct VertexInfo : IContactVertex
    {
        // add color, UV or other mesh data
        public int? index { get; set; } // vertex index
        public Color? color { get; set; }
        public Vector3? normal { get; set; }
        public Vector2? uv1 { get; set; }
        public Vector2? uv2 { get; set; }
        public Vector2? uv3 { get; set; }
        public Vector2? uv4 { get; set; }
    }

    public struct ColliderContactInfo : IContactTime, IContactMovement
    {
        public float time { get; set; }
        public Vector3? position { get; set; }
        public Vector3? velocity { get; set; }
        public Vector3? acceleration { get; set; }

        public VertexInfo target;
        public VertexInfo actor;

    }

    public class ColliderContactHistory : ContactHistory<ColliderContactInfo> { }

    abstract public class ColliderChannelOutput : ContactChannelOutput<ColliderContactInfo, ColliderContactHistory>
    {

        List<Rigidbody> targets;

        // store the colliders belonging to the target rigid bodies
        private HashSet<Collider> _ownColliders = new HashSet<Collider>();

        public bool IsTargetCollider(Collider collider)
        {
            return _ownColliders.Contains(collider);
        }

        protected override void Awake()
        {
            base.Awake();

            RefreshOwnColliders();
        }

        protected void RefreshOwnColliders()
        {
            _ownColliders.Clear();

            foreach (Rigidbody target in targets)
            {
                foreach(Collider collider in target.GetComponentsInChildren<Collider>())
                {
                    _ownColliders.Add(collider);
                }
            }
        }

    }


}

