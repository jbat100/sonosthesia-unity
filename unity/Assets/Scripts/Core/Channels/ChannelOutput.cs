
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{

    public class ChannelOutput : ChannelEndpoint
    {

        private ObjectPool<ChannelMessage> _messagePool = new ObjectPool<ChannelMessage>();

        private List<ChannelMessage> _liveMessages = new List<ChannelMessage>();

        private Dictionary<string, ChannelParameterSet> _instanceParameterSets = new Dictionary<string, ChannelParameterSet>();

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

        public ChannelParameterSet GetParameterSet(string identifier)
        {
            if (_instanceParameterSets.ContainsKey(identifier))
            {
                return _instanceParameterSets[identifier];
            }
            return null;
        }

        public void CreateInstance(string identifier, ChannelParameterSet parameters)
        {
            _instanceParameterSets[identifier] = parameters;
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.Create, identifier, parameters));
        }

        public void DestroyInstance(string identifier)
        {
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.Destroy, identifier, GetParameterSet(identifier)));
        }

        public void ControlInstance(string identifier)
        {
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.Control, identifier, GetParameterSet(identifier)));
            _instanceParameterSets.Remove(identifier);
        }

        public void StaticControl(ChannelParameterSet parameters)
        {
            controller.SendOutgoingChannelMessage(MakeChannelMessage(MessageType.Control, null, parameters));
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


