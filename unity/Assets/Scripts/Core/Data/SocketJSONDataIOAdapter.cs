using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    public class SocketJSONDataIOAdapter : DataIOAdapter
    {

        public SocketJSONMessenger messenger;


        public override void SendOutgoingChannelMessage(ChannelMessage message)
        {
            SendJSON(message.ToJSON());
        }

        public override void SendOutgoingComponentMessage(ComponentMessage message)
        {
            SendJSON(message.ToJSON());
        }

        protected override void ProcessData()
        {
            foreach (JSONObject json in messenger.DequeueMessages())
            {
                ProcessJSON(json);
            }
        }

        private void SendJSON(JSONObject json)
        {
            messenger.SendMessage(json);
        }

        private void ProcessJSON(JSONObject json)
        {
            MessageType messageType = Message.DecodeMessageType(json);

            // we aren't actually meant to receive component messages

            switch (messageType)
            {
                case MessageType.Component:
                    {
                        ComponentMessage message = new ComponentMessage();
                        message.ApplyJSON(json);
                        EmitIncomingComponentMessage(message);
                    }
                    break;
                case MessageType.Create:
                case MessageType.Destroy:
                case MessageType.Control:
                case MessageType.Event:
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


