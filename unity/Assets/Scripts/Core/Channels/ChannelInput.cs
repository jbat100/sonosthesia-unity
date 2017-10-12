using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Collections;

namespace Sonosthesia
{


    //------------------------------------------ Channel Input ---------------------------------------------

    public class ChannelInput : ChannelEndpoint, IChannelParameterDescriptionProvider
    {

        public BaseChannelFactory Factory;
        public List<GameObject> StaticRepresentations;
        public List<BaseChannelModifier> Modifiers;

        // cache the modifiers based on which key they support to save on performance
        private Dictionary<string, HashSet<BaseChannelModifier>> _modifiersByKey;


        private void BuildModifiersByKey()
        {
            if (_modifiersByKey == null)
            {
                _modifiersByKey = new Dictionary<string, HashSet<BaseChannelModifier>>();
            }
            else
            {
                _modifiersByKey.Clear();
            }

            foreach (BaseChannelModifier modifier in Modifiers)
            {
                foreach (ChannelParameterDescription description in modifier.ParameterDescriptions)
                {
                    RegisterModifierForKey(modifier, description.key);
                }
            }
        }

        private void RegisterModifierForKey(BaseChannelModifier modifier, string key)
        {
            HashSet<BaseChannelModifier> set = null;

            if (!_modifiersByKey.TryGetValue(key, out set))
            {
                set = new HashSet<BaseChannelModifier>();
                _modifiersByKey[key] = set;
            }

            set.Add(modifier);
        }

        protected virtual void OnEnable()
        {
            controller.ControlInstanceEvent += OnControlInstanceEvent;
            controller.CreateInstanceEvent += OnCreateInstanceEvent;
            controller.DestroyInstanceEvent += OnDestroyInstanceEvent;
            controller.StaticControlEvent += OnStaticControlEvent;
        }

        protected virtual void OnDisable()
        {
            controller.ControlInstanceEvent -= OnControlInstanceEvent;
            controller.CreateInstanceEvent -= OnCreateInstanceEvent;
            controller.DestroyInstanceEvent -= OnDestroyInstanceEvent;
            controller.StaticControlEvent -= OnStaticControlEvent;
        }

        protected virtual void OnStaticControlEvent(object sender, ChannelControllerStaticEventArgs e)
        {
            foreach (GameObject representation in StaticRepresentations)
            {
                ApplyParameterSet(representation, e.parameters);
            }
        }

        protected virtual void OnDestroyInstanceEvent(object sender, ChannelControllerDynamicEventArgs e)
        {
            GameObject representation = Factory.GetInstanceRepresentation(e.instance);

            ApplyParameterSet(representation, e.instance.parameters);

            Factory.DestroyInstanceRepresentation(e.instance);
        }

        protected virtual void OnCreateInstanceEvent(object sender, ChannelControllerDynamicEventArgs e)
        {
            GameObject representation = Factory.CreateInstanceRepresentation(e.instance);

            ApplyParameterSet(representation, e.instance.parameters);
        }

        protected virtual void OnControlInstanceEvent(object sender, ChannelControllerDynamicEventArgs e)
        {
            GameObject representation = Factory.GetInstanceRepresentation(e.instance);

            ApplyParameterSet(representation, e.instance.parameters);
        }

        protected virtual void ApplyParameterSet(GameObject representation, ChannelParameterSet parameters)
        {
            // using RawDict access for performance...
            foreach (KeyValuePair<string, IList<float>> kvp in parameters.RawDict)
            {
                HashSet<BaseChannelModifier> matchingModifiers = null;

                if (_modifiersByKey.TryGetValue(kvp.Key, out matchingModifiers))
                {
                    foreach (BaseChannelModifier modifier in matchingModifiers)
                    {
                        modifier.ApplyParameter(representation, kvp.Key, kvp.Value);
                    }
                }
            }
        }
    }


    //----------------------------------- Representation Factory ----------------------------------------

    public interface IChannelFactory
    {
        GameObject CreateInstanceRepresentation(ChannelInstance instance);
        void DestroyInstanceRepresentation(ChannelInstance instance);
        GameObject GetInstanceRepresentation(ChannelInstance instance);
    }

    public class BaseChannelFactory : MonoBehaviour, IChannelFactory, IChannelParameterDescriptionProvider
    {
        static ObjectCachePool<GameObject> _pool;

        public virtual IEnumerable<ChannelParameterDescription> ParameterDescriptions
        {
            get
            {
                return Enumerable.Empty<ChannelParameterDescription>();
            }
        }

        private Dictionary<string, GameObject> _liveInstances = new Dictionary<string, GameObject>();

        protected virtual Func<GameObject> FactoryConstructor()
        {
            return () => { return null; };
        }

        protected virtual Action<GameObject> FactoryReset()
        {
            return (GameObject instance) => { };
        }

        private void Awake()
        {
            _pool = new ObjectCachePool<GameObject>(1000, FactoryConstructor(), FactoryReset());
        }

        public virtual GameObject CreateInstanceRepresentation(ChannelInstance instance)
        {
            GameObject represenation = _pool.GetInstance();
            _liveInstances[instance.identifier] = represenation;
            return represenation;
        }

        public virtual GameObject GetInstanceRepresentation(ChannelInstance instance)
        {
            GameObject representation = null;
            if (!_liveInstances.TryGetValue(instance.identifier, out representation))
            {
                Debug.Log("no representation exists for instance identifier: " + instance.identifier);
            }
            return representation;
        }

        public virtual void DestroyInstanceRepresentation(ChannelInstance instance)
        {
            GameObject representation = GetInstanceRepresentation(instance);
            _liveInstances.Remove(instance.identifier);
            _pool.Release(representation);
        }


        private IEnumerator DestroyRepresentationAfterDelay(GameObject representation, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            DestroyRepresentation(representation);
        }

        public virtual void DestroyRepresentation(GameObject representation)
        {
            _pool.Release(representation);
        }

    }

    //----------------------------------- Representation Modifiers ----------------------------------------

    public interface IChannelModifier
    {
        void Initialise(GameObject representation);

        void ApplyParameter(GameObject representation, string key, IList<float> parameter);
    }
    
    abstract public class BaseChannelModifier : MonoBehaviour, IChannelModifier, IChannelParameterDescriptionProvider
    {
        abstract public void Initialise(GameObject representation);

        abstract public void ApplyParameter(GameObject representation, string key, IList<float> parameter);

        public virtual IEnumerable<ChannelParameterDescription> ParameterDescriptions
        {
            get
            {
                return Enumerable.Empty<ChannelParameterDescription>();
            }
        }

    }
    
    

}

