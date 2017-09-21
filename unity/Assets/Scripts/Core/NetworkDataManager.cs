using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    [RequireComponent(typeof(WSJSONMessenger))]
    public class NetworkDataManager : SingletonBehaviour<NetworkDataManager>
    {
        private ObjectPool<ChannelMessage> _channelMessagePool;
        private ObjectPool<ComponentMessage> _componentMessagePool;

        private ChannelMessageBuffer _channelMessageBuffer;

        private WSJSONMessenger _messenger;

        private void Awake()
        {
            _componentMessagePool = new ObjectPool<ComponentMessage>();
            _channelMessagePool = new ObjectPool<ChannelMessage>();

            _channelMessageBuffer = new ChannelMessageBuffer(_channelMessagePool);

            _messenger = GetComponent<WSJSONMessenger>();
            
        }

        // Update is called once per frame
        void Update()
        {
            if (_messenger.push)
            {
                throw new System.Exception("messenger must not be in push mode");
            }
            else
            {
                foreach(JSONObject json in _messenger.DequeueMessages())
                {
                    ProcessJSON(json);
                }
                
                foreach(ChannelMessage message in _channelMessageBuffer.DequeueMessages())
                {
                    ApplyMessage(message);
                }
            }
        }

        protected void ProcessJSON(JSONObject json)
        {
            MessageType messageType = JSONUtils.DecodeMessageType(json);

            // we aren't actually meant to receive component messages

            switch (messageType)
            {
                case MessageType.Create:
                case MessageType.Destroy:
                case MessageType.Control:
                case MessageType.Event:
                    { 
                        ChannelMessage message = _channelMessagePool.Fetch();
                        message.ApplyJSON(json);
                        _channelMessageBuffer.EnqueueMessage(message);
                    }
                    break;
                default:
                    break;
            }
        }

        protected void ApplyMessage(ChannelMessage message)
        {

        }
    }

}


