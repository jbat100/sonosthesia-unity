using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{

    public class ComponentController : MonoBehaviour
    {

        public string identifier;

        public DataIO dataIO;

        private Dictionary<string, ChannelController> _channelControllers;

        public IEnumerable<ChannelController> ChannelControllers
        {
            get
            {
                return _channelControllers.Values;
            }
        }

        public void SendOutgoingChannelMessage(ChannelMessage message)
        {
            dataIO.SendOutgoingChannelMessage(message);
        }

        public void RegisterChannelController(ChannelController controller)
        {
            if (controller != null && controller.identifier != null)
            {
                _channelControllers[controller.identifier] = controller;
            }
        }

        public void UnregisterChannelController(ChannelController controller)
        {
            if (controller != null && controller.identifier != null)
            {
                _channelControllers.Remove(controller.identifier);
            }
        }

        public void PushIncomingChannelMessage(ChannelMessage message)
        {
            ChannelController controller = null;
            if (_channelControllers.TryGetValue(message.key.channel, out controller))
            {
                controller.PushIncomingChannelMessage(message);
            }
        }

    }

}
