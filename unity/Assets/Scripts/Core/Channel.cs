using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{
	public enum ChannelFlow
	{
		UNDEFINED,
		INCOMING,
		OUTGOING,
		DUPLEX
	}

	[Serializable]
	public struct ChannelDescription
	{
		public string identifier;
		public ChannelFlow flow;
	}

	public class Channel : MonoBehaviour {

		// Use this for initialization
		void Start () {

		}

		// Update is called once per frame
		void Update () {

		}
	}


}

