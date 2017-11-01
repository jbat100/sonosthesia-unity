using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    public class SocketJSONDataIOAdapter : DataIOAdapter
    {

        [SerializeField]
        private SocketJSONMessenger _messenger;

        private static Dictionary<SocketStatus, DataIOStatus> _statusMap = new Dictionary<SocketStatus, DataIOStatus>()
        {
            { SocketStatus.UNDEFINED, DataIOStatus.UNDEFINED },
            { SocketStatus.DISCONNECTED, DataIOStatus.DISCONNECTED },
            { SocketStatus.CONNECTING, DataIOStatus.CONNECTING },
            { SocketStatus.CONNECTED, DataIOStatus.CONNECTED },
            { SocketStatus.ERROR, DataIOStatus.ERROR }
        };

        public override void DeclareComponents(IEnumerable<ComponentController> controllers)
        {
            ComponentMessage message = new ComponentMessage(controllers.Select(controller => MakeComponentInfo(controller)));

            SendOutgoingComponentMessage(message);
        }

        protected override void Awake()
        {
            base.Awake();

            if (!_messenger)
            {
                _messenger = GetComponentInChildren<SocketJSONMessenger>();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            ApplySocketStatus(_messenger.Status);

            _messenger.StatusEvent += OnSocketStatusEvent;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _messenger.StatusEvent -= OnSocketStatusEvent;
        }

        private void ApplySocketStatus(SocketStatus socketStatus)
        {
            if (_statusMap.ContainsKey(socketStatus))
            {
                Status = _statusMap[socketStatus];
            }
            else
            {
                Status = DataIOStatus.UNDEFINED;
            }
        }

        private void OnSocketStatusEvent(object sender, SocketStatusEventArgs args)
        {
            ApplySocketStatus(args.status);
        }

        public override void SendOutgoingChannelMessage(ChannelMessage message)
        {
            SendJSON(message.ToJSON());
        }

        public override void SendOutgoingComponentMessage(ComponentMessage message)
        {
            SendJSON(message.ToJSON());
        }

        private ComponentInfo MakeComponentInfo(ComponentController controller)
        {
            return new ComponentInfo(controller.identifier, controller.ChannelControllers.Select(c => MakeChannelInfo(c)));
        }

        private ChannelInfo MakeChannelInfo(ChannelController controller)
        {
            return new ChannelInfo(controller.identifier, controller.ParameterDescriptions.Select(desc => MakeParameterInfo(desc)));
        }

        private ParameterInfo MakeParameterInfo(ChannelParameterDescription desc)
        {
            return new ParameterInfo(desc.key, desc.defaultValue, desc.minValue, desc.maxValue, desc.dimensions);
        }

        protected override void ProcessData()
        {
            foreach (JSONObject json in _messenger.DequeueMessages())
            {
                ProcessJSON(json);
            }
        }

        private void SendJSON(JSONObject json)
        {
            _messenger.SendMessage(json);
        }

        private void ProcessJSON(JSONObject json)
        {
            MessageType messageType = Message.DecodeMessageType(json);

            // we aren't actually meant to receive component messages

            switch (messageType)
            {
                case MessageType.COMPONENT:
                    {
                        ComponentMessage message = new ComponentMessage();
                        message.ApplyJSON(json);
                        EmitIncomingComponentMessage(message);
                    }
                    break;
                case MessageType.CREATE:
                case MessageType.DESTROY:
                case MessageType.CONTROL:
                case MessageType.EVENT:
                    {
                        ChannelMessage message = FetchPooledChannelMessage();
                        message.ApplyJSON(json);
                        BufferChannelMessage(message);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}


