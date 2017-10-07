using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{
    public delegate void DataIOChannelMessageEventHandler(object sender, ChannelMessage channelMessage);

    public delegate void DataIOComponentMessageEventHandler(object sender, ComponentMessage componentMessage);

    public interface IDataInput
    {
        event DataIOChannelMessageEventHandler IncomingChannelMessageEvent;

        event DataIOComponentMessageEventHandler IncomingComponentMessageEvent;
    }

    public interface IDataOutput
    {
        void SendOutgoingChannelMessage(ChannelMessage message);

        void SendOutgoingComponentMessage(ComponentMessage message);
    }

    abstract public class DataIOAdapter : MonoBehaviour, IDataInput, IDataOutput
    {
        public event DataIOChannelMessageEventHandler IncomingChannelMessageEvent;

        public event DataIOComponentMessageEventHandler IncomingComponentMessageEvent;

        private ObjectPool<ChannelMessage> _channelMessagePool;
        private ChannelMessageBuffer _channelMessageBuffer;

        // store currently used channel and component messages to return them to pool on LateUpdate
        private List<ChannelMessage> _currentChannelMessages = new List<ChannelMessage>();

        abstract public void SendOutgoingChannelMessage(ChannelMessage message);

        abstract public void SendOutgoingComponentMessage(ComponentMessage message);

        abstract protected void ProcessData();

        protected virtual void Awake()
        {
            _channelMessagePool = new ObjectPool<ChannelMessage>();
            _channelMessageBuffer = new ChannelMessageBuffer(_channelMessagePool);
        }

        protected virtual void Update()
        {
            ProcessData();
            EmitIncomingChannelMessages(_channelMessageBuffer.DequeueMessages());
        }
        
        protected ChannelMessage FetchPooledChannelMessage()
        {
            return _channelMessagePool.Fetch();
        }

        protected void BufferChannelMessage(ChannelMessage message)
        {
            _channelMessageBuffer.EnqueueMessage(message);
        }

        protected void EmitIncomingComponentMessage(ComponentMessage message)
        {
            if (IncomingComponentMessageEvent != null)
            {
                IncomingComponentMessageEvent(this, message);
            }
        }

        protected void EmitIncomingChannelMessage(ChannelMessage message)
        {
            if (IncomingChannelMessageEvent != null)
            {
                _currentChannelMessages.Add(message);
                IncomingChannelMessageEvent(this, message);
            }
        }

        protected void EmitIncomingChannelMessages(IEnumerable<ChannelMessage> messages)
        {
            if (IncomingChannelMessageEvent != null)
            {
                _currentChannelMessages.AddRange(messages);
                foreach (ChannelMessage message in messages)
                {
                    IncomingChannelMessageEvent(this, message);
                }
            }
        }

        protected virtual void LateUpdate()
        {
            _channelMessagePool.Store(_currentChannelMessages);
            _currentChannelMessages.Clear();
        }

    }

    public class DataIO : MonoBehaviour, IDataInput, IDataOutput
    {
        public List<DataIOAdapter> adapters;

        public event DataIOChannelMessageEventHandler IncomingChannelMessageEvent;

        public event DataIOComponentMessageEventHandler IncomingComponentMessageEvent;

        private Dictionary<string, ComponentController> _componentControllers = new Dictionary<string, ComponentController>();

        private void OnEnable()
        {
            foreach(DataIOAdapter adapter in adapters)
            {
                adapter.IncomingChannelMessageEvent += OnIncomingChannelMessageEvent;
                adapter.IncomingComponentMessageEvent += OnIncomingComponentMessageEvent;
            }
        }

        private void OnDisable()
        {
            foreach (DataIOAdapter adapter in adapters)
            {
                adapter.IncomingChannelMessageEvent -= OnIncomingChannelMessageEvent;
                adapter.IncomingComponentMessageEvent -= OnIncomingComponentMessageEvent;
            }
        }

        public void SendOutgoingChannelMessage(ChannelMessage message)
        {
            foreach(DataIOAdapter adapter in adapters)
            {
                adapter.SendOutgoingChannelMessage(message);
            }
        }

        public void SendOutgoingComponentMessage(ComponentMessage message)
        {
            foreach (DataIOAdapter adapter in adapters)
            {
                adapter.SendOutgoingComponentMessage(message);
            }
        }

        public void RegisterComponentController(ComponentController controller)
        {
            if (controller != null && controller.identifier != null)
            {
                _componentControllers[controller.identifier] = controller;
            }
        }

        public void UnregisterComponentController(ComponentController controller)
        {
            if (controller != null && controller.identifier != null)
            {
                _componentControllers.Remove(controller.identifier);
            }
        }

        private void OnIncomingChannelMessageEvent(object sender, ChannelMessage channelMessage)
        {
            if (IncomingChannelMessageEvent != null)
            {
                IncomingChannelMessageEvent(this, channelMessage);
            }

            ComponentController controller = null;
            if (_componentControllers.TryGetValue(channelMessage.key.component, out controller))
            {
                controller.PushIncomingChannelMessage(channelMessage);
            }
        }

        private void OnIncomingComponentMessageEvent(object sender, ComponentMessage componentMessage)
        {
            if (IncomingComponentMessageEvent != null)
            {
                IncomingComponentMessageEvent(this, componentMessage);
            }
        }
    }

}

