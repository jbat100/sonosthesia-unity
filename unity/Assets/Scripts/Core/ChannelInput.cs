using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Collections;

namespace Sonosthesia
{

    //----------------------------------- Representation Factory ----------------------------------------

    public interface IChannelFactory<TRepresentation> where TRepresentation : class
    {
        TRepresentation CreateInstanceRepresentation(ChannelInstance instance);
        void DestroyInstanceRepresentation(ChannelInstance instance);
        TRepresentation GetInstanceRepresentation(ChannelInstance instance);
    }

    public class ChannelFactory<TRepresentation> : MonoBehaviour, IChannelFactory<TRepresentation> where TRepresentation : class
    {
        static ObjectCachePool<TRepresentation> _pool;

        private Dictionary<string, TRepresentation> _liveInstances = new Dictionary<string, TRepresentation>();

        protected virtual Func<TRepresentation> FactoryConstructor()
        {
            return () => { return null; };
        }

        protected virtual Action<TRepresentation> FactoryReset()
        {
            return (TRepresentation instance) => { };
        }

        private void Awake()
        {
            _pool = new ObjectCachePool<TRepresentation>(1000, FactoryConstructor(), FactoryReset());
        }

        public virtual TRepresentation CreateInstanceRepresentation(ChannelInstance instance)
        {
            TRepresentation represenation = _pool.GetInstance();
            _liveInstances[instance.identifier] = represenation;
            return represenation;
        }

        public virtual TRepresentation GetInstanceRepresentation(ChannelInstance instance)
        {
            TRepresentation representation = null;
            if (!_liveInstances.TryGetValue(instance.identifier, out representation))
            {
                Debug.Log("no representation exists for instance identifier: " + instance.identifier);
            }
            return representation;
        }

        public virtual void DestroyInstanceRepresentation(ChannelInstance instance)
        {
            TRepresentation representation = GetInstanceRepresentation(instance);
            _liveInstances.Remove(instance.identifier);
            _pool.Release(representation);
        }


        private IEnumerator DestroyRepresentationAfterDelay(TRepresentation representation, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            DestroyRepresentation(representation);
        }

        public virtual void DestroyRepresentation(TRepresentation representation)
        {
            _pool.Release(representation);
        }

    }

    //----------------------------------- Representation Modifiers ----------------------------------------

    public interface IChannelModifier<TRepresentation> : IChannelParameterDescriptionProvider where TRepresentation : class
    {
        void ApplyParameter(TRepresentation representation, string key, IList<float> parameter);
    }


    //------------------------------------------ Channel Input ---------------------------------------------

    public class ChannelInput<TRepresentation> : ChannelEndpoint, IChannelParameterDescriptionProvider where TRepresentation : class
    {
        [Tooltip("Factory used to create new instance representations")]
        public IChannelFactory<TRepresentation> factory;

        [Tooltip("Static representation")]
        public List<TRepresentation> staticRepresentations;

        public List<IChannelModifier<TRepresentation>> modifiers;

        // cache the modifiers based on which key they support to save on performance
        private Dictionary<string, HashSet<IChannelModifier<TRepresentation>>> _modifiersByKey;

        protected override void Awake()
        {
            base.Awake();

            BuildModifiersByKey();
        }

        private void BuildModifiersByKey()
        {
            if (_modifiersByKey == null)
            {
                _modifiersByKey = new Dictionary<string, HashSet<IChannelModifier<TRepresentation>>>();
            }
            else
            {
                _modifiersByKey.Clear();
            } 

            foreach (IChannelModifier<TRepresentation> modifier in modifiers)
            {
                foreach (ChannelParameterDescription description in modifier.ParameterDescriptions)
                {
                    RegisterModifierForKey(modifier, description.key);
                }
            }
        }

        private void RegisterModifierForKey(IChannelModifier<TRepresentation> modifier, string key)
        {
            HashSet<IChannelModifier<TRepresentation>> set = null;

            if (!_modifiersByKey.TryGetValue(key, out set))
            {
                set = new HashSet<IChannelModifier<TRepresentation>>();
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
            foreach(TRepresentation representation in staticRepresentations)
            {
                ApplyParameterSet(representation, e.parameters);
            }
        }

        protected virtual void OnDestroyInstanceEvent(object sender, ChannelControllerDynamicEventArgs e)
        {
            TRepresentation representation = factory.GetInstanceRepresentation(e.instance);

            ApplyParameterSet(representation, e.instance.parameters);

            factory.DestroyInstanceRepresentation(e.instance);
        }

        protected virtual void OnCreateInstanceEvent(object sender, ChannelControllerDynamicEventArgs e)
        {
            TRepresentation representation = factory.CreateInstanceRepresentation(e.instance);

            ApplyParameterSet(representation, e.instance.parameters);
        }

        protected virtual void OnControlInstanceEvent(object sender, ChannelControllerDynamicEventArgs e)
        {
            TRepresentation representation = factory.GetInstanceRepresentation(e.instance);

            ApplyParameterSet(representation, e.instance.parameters);
        }

        protected virtual void ApplyParameterSet(TRepresentation representation, ChannelParameterSet parameters)
        {
            // using RawDict access for performance...
            foreach(KeyValuePair<string, IList<float>> kvp in parameters.RawDict)
            {
                HashSet<IChannelModifier<TRepresentation>> matchingModifiers = null;
                
                if (_modifiersByKey.TryGetValue(kvp.Key, out matchingModifiers))
                {
                    foreach(IChannelModifier<TRepresentation> modifier in matchingModifiers)
                    {
                        modifier.ApplyParameter(representation, kvp.Key, kvp.Value);
                    }
                }
            }
        }
    }


}

