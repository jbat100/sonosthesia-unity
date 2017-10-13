using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Collections;

namespace Sonosthesia
{


    public class ChannelController : MonoBehaviour, IChannelParameterDescriptionProvider
    {

        private static ObjectCachePool<ChannelInstance> instanceCache = new ObjectCachePool<ChannelInstance>(1000);

        public string identifier;

        public ChannelFlow flow;

        public ComponentController componentController;

        //-----------------------------------------------------------------------------------------------

        public ChannelKey ChannelKey
        {
            get
            {
                if (componentController) return ChannelKey.Create(componentController.identifier, identifier);
                else return ChannelKey.Create(null, identifier);
            }
        } 

        [HideInInspector]
        public ChannelParameterSet staticParameters = new ChannelParameterSet();

        [HideInInspector]
        public Dictionary<string, ChannelInstance> instances = new Dictionary<string, ChannelInstance>();

        public event ChannelControllerStaticEventHandler StaticControlEvent;

        public event ChannelControllerDynamicEventHandler CreateInstanceEvent;
        public event ChannelControllerDynamicEventHandler ControlInstanceEvent;
        public event ChannelControllerDynamicEventHandler DestroyInstanceEvent;

        public IEnumerable<ChannelParameterDescription> ParameterDescriptions
        {
            get
            {
                // combine the parameter descriptions of all the endpoints
                // https://stackoverflow.com/questions/27056967/concatenate-multiple-ienumerablet

                //return _endpoints.Select(endpoint => endpoint.ParameterDescriptions).SelectMany(description => description);

                List<ChannelParameterDescription> descriptions = new List<ChannelParameterDescription>();

                foreach(ChannelEndpoint endpoint in _endpoints)
                {
                    descriptions.AddRange(endpoint.ParameterDescriptions);
                }

                return descriptions;
            }
        }

        // store for return to instanceCache on LateUpdate
        private List<ChannelInstance> _deadInstances = new List<ChannelInstance>();

        private List<ChannelEndpoint> _endpoints = new List<ChannelEndpoint>();

        private void Awake()
        {
            if (!componentController)
            {
                componentController = GetComponentInParent<ComponentController>();
            }
        }

        private void OnEnable()
        {
            if (componentController)
            {
                componentController.RegisterChannelController(this);
            }
        }

        private void OnDisable()
        {
            if (componentController)
            {
                componentController.UnregisterChannelController(this);
            }
        }

        private void LateUpdate()
        {
            foreach (ChannelInstance instance in _deadInstances)
            {
                instanceCache.Release(instance);
            }

            instances.Clear();
        }

        public void RegisterEndpoint(ChannelEndpoint endpoint)
        {
            if ((endpoint != null) && (!_endpoints.Contains(endpoint)))
            {
                _endpoints.Add(endpoint);
            }
        }

        public void UnregisterEndpoint(ChannelEndpoint endpoint)
        {
            _endpoints.Remove(endpoint);
        }

        public void PushIncomingChannelMessage(ChannelMessage message)
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

        public void SendOutgoingChannelMessage(ChannelMessage message)
        {
            componentController.SendOutgoingChannelMessage(message);
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
                _deadInstances.Add(instance);
                instances.Remove(identifier);
            }
            return instance;
        }
    }


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

        public ChannelParameterDescription Condense(ChannelParameterDescription other)
        {
            if (key != other.key)
            {
                throw new ArgumentException("mismatched keys");
            }

            return new ChannelParameterDescription(
                key,
                MathUils.Average(defaultValue, other.defaultValue),
                Mathf.Max(maxValue, other.maxValue),
                Mathf.Min(minValue, other.minValue),
                Mathf.Max(dimensions, other.dimensions) );
        }

        public static IEnumerable<ChannelParameterDescription> CondenseDescriptions(IEnumerable<ChannelParameterDescription> descriptions)
        {
            Dictionary<string, ChannelParameterDescription> condensed = new Dictionary<string, ChannelParameterDescription>();

            foreach(ChannelParameterDescription description in descriptions)
            {
                if (condensed.ContainsKey(description.key))
                {
                    condensed[description.key] = condensed[description.key].Condense(description);
                }
                else
                {
                    condensed[description.key] = description;
                }
            }

            return condensed.Values;
        }
    }

    public interface IChannelParameterDescriptionProvider
    {
        IEnumerable<ChannelParameterDescription> ParameterDescriptions { get; }
    }

    public class ChannelParameterSet
    {
        private Dictionary<string, IList<float>> _parameters = new Dictionary<string, IList<float>>();

        public void Apply(Dictionary<string, IList<float>> other)
        {
            _parameters.Combine(other);
        }

        // breaks encapsultation but better performance which is pretty critical here...
        public IDictionary<string, IList<float>> RawDict { get { return _parameters; } }

        public IEnumerable<string> GetParameterIdentifiers()
        {
            return _parameters.Keys;
        }

        public void ShallowClear()
        {
            foreach (IList<float> parameter in _parameters.Values)
            {
                parameter.Clear();
            }
        }

        public void DeepClear()
        {
            _parameters.Clear();
        }

        public void SetMultiParameter(string key, IList<float> val)
        {
            IList<float> param = EnsureParameter(key);
            param.Clear();
            (param as List<float>).AddRange(val);
        }

        public void SetParameter(string key, float val)
        {
            IList<float> param = EnsureParameter(key);
            if (param.Count == 1)
            {
                param[0] = val;
            }
            else
            {
                param.Clear();
                param.Add(val); 
            }
        }

        public void SetParameter(string key, Vector2 val)
        {
            IList<float> param = EnsureParameter(key);
            if (param.Count == 2)
            {
                param[0] = val.x;
                param[1] = val.y;
            }
            else
            {
                param.Clear();
                param.Add(val.x);
                param.Add(val.y);
            }
        }

        public void SetParameter(string key, Vector3 val)
        {
            IList<float> param = EnsureParameter(key);
            if (param.Count == 3)
            {
                param[0] = val.x;
                param[1] = val.y;
                param[2] = val.z;
            }
            else
            {
                param.Clear();
                param.Add(val.x);
                param.Add(val.y);
                param.Add(val.z);
            }
        }

        public void SetParameter(string key, Vector4 val)
        {
            IList<float> param = EnsureParameter(key);
            if (param.Count == 4)
            {
                param[0] = val.x;
                param[1] = val.y;
                param[2] = val.z;
                param[3] = val.w;
            }
            else
            {
                param.Clear();
                param.Add(val.x);
                param.Add(val.y);
                param.Add(val.z);
                param.Add(val.w);
            }
        }

        public void SetParameter(string key, Color val)
        {
            IList<float> param = EnsureParameter(key);
            if (param.Count == 4)
            {
                param[0] = val.r;
                param[1] = val.g;
                param[2] = val.b;
                param[3] = val.a;
            }
            else
            {
                param.Clear();
                param.Add(val.r);
                param.Add(val.g);
                param.Add(val.b);
                param.Add(val.a);
            }
        }

        public IList<float> GetMultiParameter(string identifier) 
        {
            return _parameters.GetValueOrDefault(identifier, null);
        }

        public float GetParameter(string identifier)
        {
            IList<float> multi = GetMultiParameter(identifier);

            // https://stackoverflow.com/questions/497261/how-do-i-get-the-first-element-from-an-ienumerablet-in-net

            return multi.FirstOrDefault();
        }

        protected IList<float> EnsureParameter(string key)
        {
            IList<float> list = null;
            if (_parameters.ContainsKey(key))
            {
                list = _parameters[key];
            }
            else
            {
                list = new List<float>();
                _parameters[key] = list;
            }
            return list;
        }
    }

    // a base class for both channel input and output objects
    public class ChannelEndpoint : MonoBehaviour, IChannelParameterDescriptionProvider
    {
        public ChannelController controller;

        public virtual IEnumerable<ChannelParameterDescription> ParameterDescriptions
        {
            get
            {
                return Enumerable.Empty<ChannelParameterDescription>();
            }
        }

        protected virtual void Awake()
        {
            controller = controller ?? GetComponentInParent<ChannelController>();
        }

        protected virtual void OnEnable()
        {
            if (controller)
            {
                controller.RegisterEndpoint(this);
            }
            else
            {
                Debug.LogError("endpoint could not find controller");
            }
        }

        protected virtual void OnDisable()
        {
            if (controller)
            {
                controller.UnregisterEndpoint(this);
            }
            else
            {
                Debug.LogError("endpoint could not find controller");
            }
        }
    }

    //--------------------------------------- Channel Controller and Events ------------------------------------------

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
    
    
}

