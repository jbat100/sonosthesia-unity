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

        // store currently used channel and component messages to return them to pool on LateUpdate
        private List<ChannelMessage> _currentChannelMessages = new List<ChannelMessage>();
        private List<ComponentMessage> _currentComponentMessages = new List<ComponentMessage>();

        private Dictionary<ChannelKey, ChannelController> _channelControllers = new Dictionary<ChannelKey, ChannelController>();

        public void SendChannelMessage(ChannelMessage message)
        {
            SendJSON(message.ToJSON());
        }

        public void SendCompnentMessage(ComponentMessage message)
        {
            SendJSON(message.ToJSON());
        }

        private void SendJSON(JSONObject json)
        {
            _messenger.SendMessage(json);
        }

        private void Awake()
        {
            _componentMessagePool = new ObjectPool<ComponentMessage>();
            _channelMessagePool = new ObjectPool<ChannelMessage>();

            _channelMessageBuffer = new ChannelMessageBuffer(_channelMessagePool);

            _messenger = GetComponent<WSJSONMessenger>();
            
        }

        private void Start()
        {
            
        }

        // Update is called once per frame
        private void Update()
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
                    ApplyChannelMessage(message);
                }
            }
        }

        private void LateUpdate()
        {
            _componentMessagePool.Store(_currentComponentMessages);
            _currentComponentMessages.Clear();

            _channelMessagePool.Store(_currentChannelMessages);
            _currentComponentMessages.Clear();
        }

        protected void ProcessJSON(JSONObject json)
        {
            MessageType messageType = JSONUtils.DecodeMessageType(json);

            // we aren't actually meant to receive component messages

            switch (messageType)
            {
                case MessageType.Component:
                    {
                        ComponentMessage message = _componentMessagePool.Fetch();
                        message.ApplyJSON(json);
                        ApplyComponentMessage(message);
                    }
                    break;
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

        protected void ApplyChannelMessage(ChannelMessage message)
        {
            _currentChannelMessages.Add(message);

            Debug.Log("ApplyChannelMessage " + message);

            ChannelKey channelKey = message.ChannelKey;

            ChannelController controller = null;

            if (_channelControllers.TryGetValue(channelKey, out controller))
            {
                controller.ApplyIncomingMessage(message);
            }
            else
            {
                Debug.LogWarning("unknown channel message key : " + channelKey);
            }
        }

        protected void ApplyComponentMessage(ComponentMessage message)
        {
            _currentComponentMessages.Add(message);

            Debug.Log("ApplyComponentMessage " + message);
        }
    }

}


