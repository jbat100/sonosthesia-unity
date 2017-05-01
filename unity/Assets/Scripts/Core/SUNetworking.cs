using UnityEngine;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;
using System;
using UnityEngine.Networking;
using UnityEngine.EventSystems;


namespace Sonosthesia
{

    public enum MessageType
    {
        Undefined,
        Component,
        Event,
        Control,
        Create,
        Destroy
    }

    public class BaseInfo
    {
        public string identifier;
    }


    public class ParameterInfo : BaseInfo
    {

    }

    public class ChannelInfo : BaseInfo
    {

    }

    public class ComponentInfo : BaseInfo
    {

    }


    public class ChanelMessage
    {
        public MessageType Type;

        public string Channel;
        public string Component;
        public string Instance;

        public Dictionary<string, string> properties;

        public Dictionary<string, float[]> parameters;
    }


    


}


