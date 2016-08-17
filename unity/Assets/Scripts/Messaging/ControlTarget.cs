using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class ControlTarget : MonoBehaviour {

	Dictionary<Type, Component> manipulators = new Dictionary<Type, Component>();

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public T GetManipulator<T>() where T : MonoBehaviour {
		// http://stackoverflow.com/questions/30396045/c-sharp-store-class-type-in-a-dictionary-list
		if (! manipulators.ContainsKey(typeof(T))) {
			// http://answers.unity3d.com/questions/43960/how-to-get-the-first-component-that-implements-a-s.html
			T manipulator = (T)GetComponent(typeof(T));
			manipulators.Add(typeof(T), manipulator);
		}
		return (T)manipulators[typeof(T)];
	}
}

public interface IControlTargetProvider {

	List<ControlTarget> GetTargets();

}