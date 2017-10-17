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

        public override IEnumerable<ChannelParameterDescription> ParameterDescriptions { get { return _parameterDescriptions; } }

        static private IEnumerable<ChannelParameterDescription> _parameterDescriptions = new List<ChannelParameterDescription>()
        {
            new ChannelParameterDescription(ContactChannelParameters.KEY_POSITION, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(ContactChannelParameters.KEY_VELOCITY, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(ContactChannelParameters.KEY_ACCELERATION, 0f, 1f, 0f, 3),

            new ChannelParameterDescription(ContactChannelParameters.KEY_TARGET_NORMAL, 0f, 1f, 0f, 3),
            new ChannelParameterDescription(ContactChannelParameters.KEY_TARGET_UV1, 0f, 1f, 0f, 2),
            new ChannelParameterDescription(ContactChannelParameters.KEY_TARGET_UV2, 0f, 1f, 0f, 2),
        };


        abstract protected Vector3? GetScreenPosition(int contactId);

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
            Vector3? nullablePosition = GetScreenPosition(contactId);

            if (nullablePosition == null)
            {
                return null;
            }

            RaycastHit? nullableHit = Raycast(nullablePosition.Value);

            if (nullableHit != null)
            {
                RaycastHit hit = nullableHit.Value;
                ColliderContactInfo info = new ColliderContactInfo();
                info.position = hit.point;
                info.time = Time.unscaledTime;
                info.target.uv1 = hit.textureCoord;
                info.target.uv2 = hit.textureCoord2;
                info.target.normal = hit.normal;
                
                // TODO use triangleIndex to get extra info from the collision vertices

                return info;
            }

            return null;
            
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

            RaycastHit? result = null;

            if (ignoreOcclusion)
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layerMask, QueryTriggerInteraction.Collide);

                foreach (RaycastHit hit in hits)
                {
                    if (IsTargetCollider(hit.collider))
                    {
                        result = hit;
                        break;
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
                        result = hit;
                    }
                }
            }

            return result;
        }

    }

}
