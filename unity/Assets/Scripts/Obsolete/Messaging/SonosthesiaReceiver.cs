using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SonosthesiaReceiver : MonoBehaviour {


	public List<string> groups = new  List<string> {"default"};

	private List<SonosthesiaResponder> responders;

	void Awake () {

		responders = GetComponents<SonosthesiaResponder>().ToList();
		
	}

	public void ApplyMessage(SonosthesiaControlMessage message) {
		
		// should we apply messages for this group?
		var result = string.Join(", ", message.values.ToArray().Select(o => o.ToString()).ToArray());
		if (groups.Contains(message.group)) {
			//Debug.Log (this.GetType().Name + " applying " + message.ToString() + " with values : " + result );
			foreach(SonosthesiaResponder responder in responders) {
				responder.ApplyMessage(message);
			}
			return;
		} else {
			//Debug.Log (this.GetType().Name + " not applying " + message.ToString() + " with values : " + result );
		}

	}


}
