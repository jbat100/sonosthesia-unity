using UnityEngine;
using System;
using System.Collections;

public class ClockAnimator : MonoBehaviour {

	public Transform hours, minutes, seconds;

	public bool analog;

	private const float hoursToDegrees = 360f / 12f;
	private const float minutesToDegrees = 360f / 60f;
	private const float secondsToDegrees = 360f / 60f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (analog) {
			// currently do nothing
			TimeSpan timespan = DateTime.Now.TimeOfDay;
			hours.localRotation = Quaternion.Euler(0f, 0f, (float)timespan.TotalHours * -hoursToDegrees);
			minutes.localRotation = Quaternion.Euler(0f, 0f, (float)timespan.TotalMinutes * -minutesToDegrees);
			seconds.localRotation = Quaternion.Euler(0f, 0f, (float)timespan.TotalSeconds * -secondsToDegrees);
		}
		else {
			DateTime time = DateTime.Now;
			hours.localRotation = Quaternion.Euler(0f, 0f, time.Hour * -hoursToDegrees);
			minutes.localRotation = Quaternion.Euler(0f, 0f, time.Minute * -minutesToDegrees);
			seconds.localRotation = Quaternion.Euler(0f, 0f, time.Second * -secondsToDegrees);
		}
	}
}
