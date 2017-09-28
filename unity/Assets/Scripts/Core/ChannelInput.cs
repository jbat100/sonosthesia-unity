using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Collections;

namespace Sonosthesia
{

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

        public TRepresentation CreateInstanceRepresentation(ChannelInstance instance)
        {
            TRepresentation represenation = _pool.GetInstance();
            _liveInstances[instance.identifier] = represenation;
            return represenation;
        }

        public void DestroyInstanceRepresentation(ChannelInstance instance)
        {
            TRepresentation representation = GetInstanceRepresentation(instance);
            _liveInstances.Remove(instance.identifier);
            _pool.Release(representation);
        }

        public TRepresentation GetInstanceRepresentation(ChannelInstance instance)
        {
            TRepresentation representation = null;
            if (!_liveInstances.TryGetValue(instance.identifier, out representation))
            {
                Debug.Log("no representation exists for instance identifier: " + instance.identifier);
            }
            return representation;
        }

    }

    public class ChannelInput<TRepresentation> : ChannelEndpoint, IChannelParameterDescriptionProvider
    {

        protected override void Awake()
        {
            base.Awake();

            
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

        protected virtual void OnStaticControlEvent(object sender, ChannelControllerStaticEventArgs e) { }

        protected virtual void OnDestroyInstanceEvent(object sender, ChannelControllerDynamicEventArgs e) { }

        protected virtual void OnCreateInstanceEvent(object sender, ChannelControllerDynamicEventArgs e) { }

        protected virtual void OnControlInstanceEvent(object sender, ChannelControllerDynamicEventArgs e) { }


    }


}

