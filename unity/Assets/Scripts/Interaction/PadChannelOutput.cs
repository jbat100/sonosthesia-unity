using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{
    

    public class PadContactHistory : ContactHistory<ContactInfo> { }

    abstract public class PadChannelOutput : ContactChannelOutput<ContactInfo, PadContactHistory>
    {
        public RectTransform pad;

        public Camera rayCamera;

        public float maxPressure = 10f;
        public float maxSpeed = 10f;
        public float maxAcceleration = 10f;

        public override bool IsInteractive { get { return base.IsInteractive && pad && pad.gameObject.activeSelf && pad.gameObject.activeInHierarchy; } }

        protected override void Awake()
        {
            base.Awake();

            if (!rayCamera)
            {
                rayCamera = Camera.main;
            }
        }

        protected override void ApplyContactHistoryToInstance(PadContactHistory history, ChannelInstance instance)
        {
            instance.parameters.SetParameter(ContactChannelParameters.KEY_POSITION, history.Position);
            instance.parameters.SetParameter(ContactChannelParameters.KEY_VELOCITY, history.Velocity);
            instance.parameters.SetParameter(ContactChannelParameters.KEY_ACCELERATION, history.Acceleration);
        }

        protected virtual bool ScreenPointIsInPanel(Vector3 position)
        {
            if (!pad)
            {
                return false;
            }

            Canvas canvas = pad.GetComponentInParent<Canvas>();

            // https://forum.unity3d.com/threads/whats-wrong-with-recttransformutility-rectanglecontainsscreenpoint-camera-argument.328618/
            Camera cam = (canvas && (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)) ? rayCamera : null;

            bool result = RectTransformUtility.RectangleContainsScreenPoint(pad, position, rayCamera);

            return result;
        }
    }


}

