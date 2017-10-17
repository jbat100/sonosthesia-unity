using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{

    abstract public class RayChannelOutput : ColliderChannelOutput
    {

        public LayerMask layerMask;

        public bool ignoreOcclusion;

        public Camera raycastCamera;

        // used to get collider list
        private Dictionary<int, RaycastHit> _hitList = new Dictionary<int, RaycastHit>();

        abstract protected Vector3 GetScreenPosition(int contactId);

        protected override void Awake()
        {
            base.Awake();

            if (!raycastCamera)
            {
                raycastCamera = Camera.main;
            }
        }
        
        protected override ColliderContactInfo? GetContactInfo(int contactId)
        {
            ColliderContactInfo info = new ColliderContactInfo();
            info.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f);
            info.time = Time.unscaledTime;
            return info;
        }

        protected override void ApplyContactHistoryToInstance(ColliderContactHistory history, ChannelInstance instance)
        {
            instance.parameters.SetParameter(ContactChannelParameters.KEY_POSITION, history.Position);
            instance.parameters.SetParameter(ContactChannelParameters.KEY_VELOCITY, history.Velocity);
            instance.parameters.SetParameter(ContactChannelParameters.KEY_ACCELERATION, history.Acceleration);
        }

        protected virtual RaycastHit? Raycast(Vector2 position)
        {
            Ray ray = raycastCamera.ScreenPointToRay(position);

            if (ignoreOcclusion)
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide);

                foreach (RaycastHit hit in hits)
                {
                    if (IsTargetCollider(hit.collider))
                    {
                        return hit;
                    }
                }
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide))
                {
                    if (IsTargetCollider(hit.collider))
                    {
                        return hit;
                    }
                }
            }
            
            return null;
        }

    }

}
