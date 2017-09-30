using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class MaterialManipulatorHelper : ManipulatorHelper {

	// according to a 153k (god level) stack overflow user a property named the same as a class is fine
	// http://stackoverflow.com/questions/1095644/should-a-property-have-the-same-name-as-its-type

	public Renderer Renderer { get { return renderer; } }

	private Renderer renderer;

	public MaterialManipulatorHelper(GameObject _target) : base (_target){
		renderer = Target.GetComponent<Renderer> ();
	}

	public Material GetMaterial(int materialIndex) {
		if (renderer.materials.Length > materialIndex) {
			return renderer.materials[materialIndex];
		}
		return renderer.material;
	}
}
