
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{

    public class ChannelOutput : ChannelEndpoint
    {

        public ChannelParameterSet StaticParameterSet { get { return staticParameterSet; } }

        private static ObjectPool<ChannelMessage> _messagePool = new ObjectPool<ChannelMessage>();
        private static ObjectPool<ChannelInstance> _channelInstancePool = new ObjectPool<ChannelInstance>();

        private List<ChannelMessage> _liveMessages = new List<ChannelMessage>();
        private Dictionary<string, ChannelInstance> _instances = new Dictionary<string, ChannelInstance>();
        private ChannelParameterSet staticParameterSet = new ChannelParameterSet();

        public static string CreateInstanceIdentifier()
        {
            return CommonUtils.MakeGUID();
        }

        private void LateUpdate()
        {
            // return live messages to the pool
            foreach(ChannelMessage message in _liveMessages)
            {
                _messagePool.Store(message);
            }
            _liveMessages.Clear();
        }

        public ChannelInstance GetInstance(string identifier)
        {
            if (_instances.ContainsKey(identifier))
            {
                return _instances[identifier];
            }
            return null;
        }

        // fetches an instance which is available for reuse or creates a new one
        public ChannelInstance FetchChannelInstance()
        {
            ChannelInstance instance = _channelInstancePool.Fetch();
            instance.identifier = CreateInstanceIdentifier();
            instance.parameters.DeepClear();
            return instance;
        }

        // returns an instance for reuse
        public void StoreChannelInstance(ChannelInstance instance)
        {
            _channelInstancePool.Store(instance);
        }

        public void CreateInstance(ChannelInstance instance)
        {
            _instances[instance.identifier] = instance;
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.CREATE, instance));
        }

        public void DestroyInstance(ChannelInstance instance)
        {
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.DESTROY, instance));
        }

        public void ControlInstance(ChannelInstance instance)
        {
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.CONTROL, instance));
            _instances.Remove(instance.identifier);
        }

        public void StaticControl(ChannelParameterSet parameters)
        {
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.CONTROL, null, parameters));
        }

        private ChannelMessage MakeChannelMessage(MessageType messageType, ChannelInstance instance)
        {
            return MakeChannelMessage(messageType, instance.identifier, instance.parameters);
        }

        private ChannelMessage MakeChannelMessage(MessageType messageType, string identifier, ChannelParameterSet parameters)
        {
            ChannelMessage message = _messagePool.Fetch();
            message.type = messageType;
            message.key = MakeInstanceKey(identifier);
            message.parameters = parameters.RawDict as Dictionary<string, IList<float>>;

            _liveMessages.Add(message);

            return message;
        }

        private ChannelInstanceKey MakeInstanceKey(string identifier)
        {
            ChannelKey channelKey = controller.ChannelKey;
            return ChannelInstanceKey.Create(channelKey.component, channelKey.channel, identifier);
        }

    }

}


