
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class SonosthesiaResponder : MonoBehaviour {


	protected delegate void ResponderDelegate(ArrayList values);

	protected Dictionary<string, ResponderDelegate> responderDelegates = new Dictionary<string, ResponderDelegate>();

	//protected string Tag = "";


	public virtual void Awake() {

		//Tag = this.GetType().Name;

	}

	public virtual void Start() {

	}
		

	public virtual void ApplyMessage(SonosthesiaControlMessage message) {

		ResponderDelegate responderDelegate = null;
		//Debug.Log (this.GetType().Name + " responder delegate keys: " + String.Join(", ", responderDelegates.Keys.ToArray()) );
		if (responderDelegates.TryGetValue(message.descriptor, out responderDelegate)) {
			//Debug.Log (this.GetType().Name + " found responder delegate for descriptor " + message.descriptor );
			// call the main delegate once
			responderDelegate(message.values);
		} else {
			//Debug.Log (this.GetType().Name + " no responder delegate for descriptor " + message.descriptor );
		}

	}

}
