using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System;

public class SonosthesiaControlMessage
{
	public string descriptor;
	public string group;

	public ArrayList values;

	public SonosthesiaControlMessage(string _group, string _descriptor, ArrayList _values) {
		descriptor = _descriptor;
		group = _group;
		values = _values;
	}

	public override string ToString()
	{
		//string valuesString = string.Join(",", values.Select(x => x.ToString()).ToArray());
		// string valuesString = String.Join(",", values.ToArray());
		//Debug.Log("group is " + group);
		//string valuesString = String.Join(",", (string[]) values.ToArray());

		return "SonosthesiaControlMessage with descriptor: " + descriptor + ", group: " + group;
	}

	public bool sameTarget(SonosthesiaControlMessage message) {
		if (!message.descriptor.Equals (descriptor) || !message.group.Equals (group)) {
			return false;
		}
		return true;
	}

	public static SonosthesiaControlMessage FromOscMessage(OscMessage message) {

		string[] elements = message.Address.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

		//Debug.Log("RoutingParametersForMessage elements are " + elements.ToString());

		if (elements.Length < 2) 
		{
			Debug.Log("DefaultMessageCallback unexpected elements length");
			return null;
		}

		return new SonosthesiaControlMessage (elements [0], elements [1], message.Values);
	}
};

public class SonosthesiaControlMessageQueue {

	private readonly object syncLock = new object();
	private List<SonosthesiaControlMessage> queue;

	public int Count
	{
		get
		{
			lock(syncLock) 
			{
				return queue.Count;
			}
		}
	}

	public SonosthesiaControlMessageQueue()
	{
		this.queue = new List<SonosthesiaControlMessage>();
	}

	public SonosthesiaControlMessage Peek()
	{
		lock(syncLock)
		{
			return queue[0];
		}
	}	

	private void UnprotectedEnqueue (SonosthesiaControlMessage message) {

		int index = -1;
		for (int i = 0; i < queue.Count; i++)
		{
			SonosthesiaControlMessage m = queue[i];
			if (m.sameTarget(message)) 
			{
				index = i;
				break;
			}
		}
		if (index == -1) 
		{
			queue.Add(message);
		} 
		else 
		{
			queue[index] = message;
		}
		
	}

	public void Enqueue(SonosthesiaControlMessage message)
	{
		lock(syncLock)
		{
			this.UnprotectedEnqueue(message);
		}
	}

	public void EnqueueList(List<SonosthesiaControlMessage> messages) {

		lock(syncLock)
		{
			if (messages.Count > 0) {
				Debug.Log("Enqueuing " + messages.Count + " control message(s)");
			}

			foreach(SonosthesiaControlMessage message in messages)
			{
				this.UnprotectedEnqueue(message);
			}
		}

	}

	public void TransferFrom(SonosthesiaControlMessageQueue otherQueue)
	{
		List<SonosthesiaControlMessage> transfered = otherQueue.DequeueAll();

		this.EnqueueList(transfered);

	}

	public SonosthesiaControlMessage Dequeue()
	{
		lock(syncLock)
		{
			if (queue.Count > 0) {
				SonosthesiaControlMessage message = queue[0];
				queue.RemoveAt(0);
				return message;
			}
			return null;
		}
	}

	public List<SonosthesiaControlMessage> DequeueAll()
	{
		lock(syncLock)
		{
			List<SonosthesiaControlMessage> copied = new List<SonosthesiaControlMessage>(queue);			
			queue.Clear();
			return copied;
		}
	}

	public void Clear()
	{
		lock(syncLock)
		{
			queue.Clear();
		}
	}

	public SonosthesiaControlMessage[] CopyToArray()
	{
		lock(syncLock)
		{
			if(queue.Count == 0)
			{
				return new SonosthesiaControlMessage[0];
			}

			SonosthesiaControlMessage[] values = new SonosthesiaControlMessage[queue.Count];
			queue.CopyTo(values, 0);	
			return values;
		}
	}

	public static SonosthesiaControlMessageQueue InitFromArray(IEnumerable<SonosthesiaControlMessage> initValues)
	{
		var queue = new SonosthesiaControlMessageQueue();

		if(initValues == null)	
		{
			return queue;
		}

		foreach(SonosthesiaControlMessage val in initValues)
		{
			queue.Enqueue(val);
		}

		return queue;
	}
};
	

public class SonosthesiaControlMessageBuffer {

	private Osc handler;

	private SonosthesiaControlMessageQueue bufferQueue;
	private SonosthesiaControlMessageQueue targetQueue;

	private Timer timer;
	private int period;

	public SonosthesiaControlMessageBuffer(Osc _handler, SonosthesiaControlMessageQueue _targetQueue, int _period) {
		
		bufferQueue = new SonosthesiaControlMessageQueue ();
		targetQueue = _targetQueue;
		handler = _handler;
		handler.SetAllMessageHandler(MessageCallback);
		period = _period;

		timer = new Timer(TransferCallback, null, period, period);
	}

	public void Stop() {

		// stop it permanently with no further chance to use the same instance
		timer.Dispose();

		//timer.Change(Timeout.Infinite , Timeout.Infinite);

	}

	public void MessageCallback (OscMessage oscMessage) {
		
		//Debug.Log("DefaultMessageCallback received message " + message.Address + ' ' + message.Values[0]);
		SonosthesiaControlMessage SonosthesiaMessage = SonosthesiaControlMessage.FromOscMessage (oscMessage);
		if (SonosthesiaMessage != null) 
		{
			//Loging here is too expensive when receiving kHz + message frequencies
			//Debug.Log("Received message : " + SonosthesiaMessage.ToString() );
			bufferQueue.Enqueue(SonosthesiaMessage);
		}

	}

	public void TransferCallback(object state) {

		targetQueue.TransferFrom(bufferQueue);

	}

};

public class SonosthesiaOSCManager : MonoBehaviour {

	public string remoteIP = "127.0.0.1";
	public int remotePort = 9123;
	public int localPort = 3333;

	private Osc handler;
	private SonosthesiaControlMessageBuffer messageBuffer;
	private SonosthesiaControlMessageQueue messageQueue;

	private List<SonosthesiaReceiver> receivers;
	private int updateCount = 0;


	// Use this for initialization
	void Start () {

		//Initializes on start up to listen for messages
		//make sure this game object has both UDPPackIO and OSC script attached

		UDPPacketIO udp = (UDPPacketIO)GetComponent("UDPPacketIO");
		udp.init(remoteIP, remotePort, localPort);
		handler = (Osc)GetComponent("Osc");
		handler.init(udp);

		messageQueue = new SonosthesiaControlMessageQueue ();
		messageBuffer = new SonosthesiaControlMessageBuffer(handler, messageQueue, 17);

		// get all game objects with maniulator components

		receivers = FindObjectsOfType<SonosthesiaReceiver>().ToList();

		Debug.Log("SonosthesiaOSCManager found " + receivers.Count + " receivers(s)");

		// not sure if InvokeRepeating does what we want, might need to create a new thread
		// http://stackoverflow.com/questions/12997658/c-sharp-how-to-make-periodic-events-in-a-class
		// InvokeRepeating("ReadBuffer", period, period);

	}

	// Update is called once per frame
	void Update () {

		updateCount++;

		List<SonosthesiaControlMessage> dequeued = messageQueue.DequeueAll();

		foreach (SonosthesiaControlMessage message in dequeued) {
			Debug.Log("SonosthesiaOSCManager applying message : " + message.ToString());
			foreach (SonosthesiaReceiver receiver in receivers) {
				receiver.ApplyMessage(message);
			}
		}

	}

	void OnApplicationQuit() {

		// proactively close otherwise the thread goes on and needs to be hard killed 

		UDPPacketIO udp = (UDPPacketIO)GetComponent("UDPPacketIO");
		udp.Close ();

		messageBuffer.Stop();

	}
};
