using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Collections;

namespace Sonosthesia
{
	public enum ChannelFlow
	{
		UNDEFINED,
		INCOMING,
		OUTGOING,
		DUPLEX
	}

    [Serializable]
    public class ChannelParameterDescription
    {
        public string key;
        public float defaultValue;
        public float maxValue;
        public float minValue;
        public int dimensions;

        public ChannelParameterDescription(string _key, float _defaultValue = 0f, float _maxValue = 1f, float _minValue = 0f, int _dimensions = 1)
        {
            key = _key;
            defaultValue = _defaultValue;
            maxValue = _maxValue;
            minValue = _minValue;
            dimensions = _dimensions;
        }
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

        public ChannelControllerStaticEventArgs(ChannelParameterSet _parameters)
        {
            parameters = _parameters;
        }
    }
    
    public delegate void ChannelControllerStaticEventHandler(object sender, ChannelControllerStaticEventArgs e);

    public struct ChannelControllerDynamicEventArgs
    {
        public ChannelInstance instance;

        public ChannelControllerDynamicEventArgs(ChannelInstance _instance)
        {
            instance = _instance;
        }
    }

    public delegate void ChannelControllerDynamicEventHandler(object sender, ChannelControllerDynamicEventArgs e);

    public class ChannelController : MonoBehaviour {

        private static ObjectCachePool<ChannelInstance> instanceCache = new ObjectCachePool<ChannelInstance>(1000); 

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

        // store for return to instanceCache on LateUpdate
        private List<ChannelInstance> deadInstances = new List<ChannelInstance>();

        private void Awake()
        {
            if (!componentController)
            {
                componentController = GetComponentInParent<ComponentController>();
            }
        }

        private void LateUpdate()
        {
            foreach(ChannelInstance instance in deadInstances)
            {
                instanceCache.Release(instance);
            }

            instances.Clear();
        }

        public void ApplyIncomingMessage(ChannelMessage message)
        {
            if (message.key.component != componentController.identifier || message.key.channel != identifier)
            {
                throw new Exception("mismatched message");
            }

            if (message.key.instance != null)
            {
                switch (message.type)
                {
                    case MessageType.Control:
                        {
                            ChannelInstance instance = EnsureChannelInstance(message.key.instance);
                            ExtractParameterSet(instance.parameters, message);
                            if (ControlInstanceEvent != null)
                            {
                                ControlInstanceEvent(this, new ChannelControllerDynamicEventArgs(instance));
                            }
                        }
                        break;
                    case MessageType.Create:
                        {
                            ChannelInstance instance = EnsureChannelInstance(message.key.instance);
                            ExtractParameterSet(instance.parameters, message);
                            if (CreateInstanceEvent != null)
                            {
                                CreateInstanceEvent(this, new ChannelControllerDynamicEventArgs(instance));
                            }
                        }
                        break;
                    case MessageType.Destroy:
                        {
                            ChannelInstance instance = CleanChannelInstance(message.key.instance);
                            ExtractParameterSet(instance.parameters, message);
                            if (DestroyInstanceEvent != null)
                            {
                                DestroyInstanceEvent(this, new ChannelControllerDynamicEventArgs(instance));
                            }
                        }
                        break;
                    default:
                        Debug.LogWarning("unepexted message type: " + message.type);
                        break;
                }
            }
            else
            {
                switch (message.type)
                {
                    case MessageType.Control:
                        {
                            ExtractParameterSet(staticParameters, message);
                            if (StaticControlEvent != null)
                            {
                                StaticControlEvent(this, new ChannelControllerStaticEventArgs(staticParameters));
                            }
                        }
                        break;
                }
            }
        }

        public void SendOutgoingMessage(ChannelMessage message)
        {

        }

        private void ExtractParameterSet(ChannelParameterSet parameterSet, ChannelMessage message)
        {
            parameterSet.Apply(message.parameters);
        }

        private ChannelInstance EnsureChannelInstance(string identifier)
        {
            ChannelInstance instance = null;
            if (!instances.TryGetValue(identifier, out instance))
            {
                instance = instanceCache.GetInstance();
                instances[identifier] = instance;
            }
            return instance;
        }

        private ChannelInstance CleanChannelInstance(string identifier)
        {
            ChannelInstance instance = null;
            if (instances.TryGetValue(identifier, out instance))
            {
                deadInstances.Add(instance);
                instances.Remove(identifier);
            }
            return instance;
        }
    }
    
}

