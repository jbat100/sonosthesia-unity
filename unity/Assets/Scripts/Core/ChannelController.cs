using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{
	public enum ChannelFlow
	{
		UNDEFINED,
		INCOMING,
		OUTGOING,
		DUPLEX
	}

    public class ChannelParameterSet
    {
        private Dictionary<string, IEnumerable<float>> _parameters = new Dictionary<string, IEnumerable<float>>();

        public void Apply(Dictionary<string, IEnumerable<float>> other)
        {
            _parameters.Combine(other);
        }

        public IEnumerable<float> GetMultiParameter(string identifier) 
        {
            return _parameters.GetValueOrDefault(identifier, null);
        }

        public float GetParameter(string identifier)
        {
            IEnumerable<float> multi = GetMultiParameter(identifier);

            // https://stackoverflow.com/questions/497261/how-do-i-get-the-first-element-from-an-ienumerablet-in-net

            return multi.FirstOrDefault();
        }
    }

    public class ChannelInstance
    {
        public string identifier;
        public ChannelParameterSet parameters = new ChannelParameterSet();
    }

    public struct ChannelControllerStaticEventArgs
    {
        public ChannelParameterSet parameters;
    }
    
    public delegate void ChannelControllerStaticEventHandler(object sender, ChannelControllerStaticEventArgs e);

    public struct ChannelControllerDynamicEventArgs
    {
        public ChannelInstance instance;
    }

    public delegate void ChannelControllerDynamicEventHandler(object sender, ChannelControllerDynamicEventArgs e);

    public class ChannelController : MonoBehaviour {

        public string identifier;

        public ChannelFlow flow;

        public ComponentController componentController;

        //-----------------------------------------------------------------------------------------------

        [HideInInspector]
        public ChannelParameterSet staticParameters = new ChannelParameterSet();

        [HideInInspector]
        public Dictionary<string, ChannelInstance> instances = new Dictionary<string, ChannelInstance>();

        public event ChannelControllerStaticEventHandler StaticControlEvent;

        public event ChannelControllerDynamicEventHandler CreateInstanceEvent;
        public event ChannelControllerDynamicEventHandler ControlInstanceEvent;
        public event ChannelControllerDynamicEventHandler DestroyInstanceEvent;

        private void Awake()
        {
            if (!componentController)
            {
                componentController = GetComponentInParent<ComponentController>();
            }
        }

        public void ApplyIncomingMessage(ChannelMessage message)
        {
            if (message.key.component != componentController.identifier || message.key.channel != identifier)
            {
                throw new Exception("mismatched message");
            }

            if (message.key.instance != null)
            {

            }

        }

        public void SendOutgoingMessage(ChannelMessage message)
        {

        }
    }
    
}

