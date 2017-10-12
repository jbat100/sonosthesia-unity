using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Sonosthesia
{
    public delegate void DataIOChannelMessageEventHandler(object sender, ChannelMessage channelMessage);

    public delegate void DataIOComponentMessageEventHandler(object sender, ComponentMessage componentMessage);

    
    public class DataIO : DataIOBase
    {
        public List<DataIOAdapter> adapters;

        public IEnumerable<ComponentController> ComponentControllers { get { return _componentControllers.Values; } }

        private Dictionary<string, ComponentController> _componentControllers = new Dictionary<string, ComponentController>();

        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (DataIOAdapter adapter in adapters)
            {
                adapter.IncomingChannelMessageEvent += OnIncomingChannelMessageEvent;
                adapter.IncomingComponentMessageEvent += OnIncomingComponentMessageEvent;
                adapter.StatusEvent += OnAdapterStatusEvent;
            }
        }


        protected override void OnDisable()
        {
            base.OnDisable();

            foreach (DataIOAdapter adapter in adapters)
            {
                adapter.IncomingChannelMessageEvent -= OnIncomingChannelMessageEvent;
                adapter.IncomingComponentMessageEvent -= OnIncomingComponentMessageEvent;
                adapter.StatusEvent -= OnAdapterStatusEvent;
            }
        }

        private void OnAdapterStatusEvent(object sender, DataIOStatueEnventArgs args)
        {
            if (args.status == DataIOStatus.CONNECTED)
            {
                DataIOAdapter adapter = sender as DataIOAdapter;
                if (adapter)
                {
                    adapter.DeclareComponents(_componentControllers.Values);
                }
            }
        }


        public override void SendOutgoingChannelMessage(ChannelMessage message)
        {
            foreach (DataIOAdapter adapter in adapters)
            {
                adapter.SendOutgoingChannelMessage(message);
            }
        }

        public override void SendOutgoingComponentMessage(ComponentMessage message)
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
            EmitIncomingChannelMessage(channelMessage);

            ComponentController controller = null;
            if (_componentControllers.TryGetValue(channelMessage.key.component, out controller))
            {
                controller.PushIncomingChannelMessage(channelMessage);
            }
        }

        private void OnIncomingComponentMessageEvent(object sender, ComponentMessage componentMessage)
        {
            EmitIncomingComponentMessage(componentMessage);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(DataIO))]
    public class DataIOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Show default inspector property editor
            DrawDefaultInspector();

            DataIO dataIO = (DataIO)target;

            if (GUILayout.Button("Declare Components"))
            {
                foreach (DataIOAdapter adapter in dataIO.adapters)
                {
                    adapter.DeclareComponents(dataIO.ComponentControllers);
                }
            }

        }
    }

#endif


    public enum DataIOStatus
    {
        UNDEFINED,
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
        ERROR
    }

    public struct DataIOStatueEnventArgs
    {
        public DataIOStatus status;
        public DataIOStatus previous;

        public DataIOStatueEnventArgs(DataIOStatus _status, DataIOStatus _previous = DataIOStatus.UNDEFINED)
        {
            status = _status;
            previous = _previous;
        }
    }


    public delegate void DataIOStatusEnventHandler(object sender, DataIOStatueEnventArgs args);

    public interface IDataInput
    {
        event DataIOStatusEnventHandler StatusEvent;

        event DataIOChannelMessageEventHandler IncomingChannelMessageEvent;

        event DataIOComponentMessageEventHandler IncomingComponentMessageEvent;
    }

    public interface IDataOutput
    {
        void SendOutgoingChannelMessage(ChannelMessage message);

        void SendOutgoingComponentMessage(ComponentMessage message);
    }

    abstract public class DataIOBase : MonoBehaviour, IDataInput, IDataOutput
    {
        public event DataIOStatusEnventHandler StatusEvent;

        public event DataIOChannelMessageEventHandler IncomingChannelMessageEvent;

        public event DataIOComponentMessageEventHandler IncomingComponentMessageEvent;

        abstract public void SendOutgoingChannelMessage(ChannelMessage message);

        abstract public void SendOutgoingComponentMessage(ComponentMessage message);

        public DataIOStatus Status
        {
            get
            {
                return _status;
            }
            protected set
            {
                _status = value;
                EmitStatus(value);
            }
        }

        private DataIOStatus _status = DataIOStatus.UNDEFINED;

        protected virtual void OnEnable()
        {
            StatusEvent += OnStatusEvent;
        }

        protected virtual void OnDisable()
        {
            StatusEvent -= OnStatusEvent;
        }

        protected virtual void OnStatusEvent(object sender, DataIOStatueEnventArgs args)
        {
            // override this to take action on status change
        }

        protected virtual void EmitIncomingComponentMessage(ComponentMessage message)
        {
            if (IncomingComponentMessageEvent != null)
            {
                IncomingComponentMessageEvent(this, message);
            }
        }

        protected virtual void EmitStatus(DataIOStatus status)
        {
            if (StatusEvent != null)
            {
                StatusEvent(this, new DataIOStatueEnventArgs(status));
            }
        }

        protected virtual void EmitIncomingChannelMessage(ChannelMessage message)
        {
            if (IncomingChannelMessageEvent != null)
            {
                IncomingChannelMessageEvent(this, message);
            }
        }

        protected virtual void EmitIncomingChannelMessages(IEnumerable<ChannelMessage> messages)
        {
            if (IncomingChannelMessageEvent != null)
            {
                foreach (ChannelMessage message in messages)
                {
                    IncomingChannelMessageEvent(this, message);
                }
            }
        }
    }

    abstract public class DataIOAdapter : DataIOBase
    {

        private ObjectPool<ChannelMessage> _channelMessagePool;
        private ChannelMessageBuffer _channelMessageBuffer;

        // store currently used channel and component messages to return them to pool on LateUpdate
        private List<ChannelMessage> _currentChannelMessages = new List<ChannelMessage>();
        
        abstract protected void ProcessData();

        abstract public void DeclareComponents(IEnumerable<ComponentController> controllers);

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

        protected override void EmitIncomingChannelMessage(ChannelMessage message)
        {
            base.EmitIncomingChannelMessage(message);
            _currentChannelMessages.Add(message);
        }

        protected override void EmitIncomingChannelMessages(IEnumerable<ChannelMessage> messages)
        {
            base.EmitIncomingChannelMessages(messages);
            _currentChannelMessages.AddRange(messages);
        }


        protected virtual void LateUpdate()
        {
            _channelMessagePool.Store(_currentChannelMessages);
            _currentChannelMessages.Clear();
        }

    }


}

