using UnityEngine;
using System.Collections;

public class ShurikenColorManipulator : ColorManipulator {

	private ShurikenManipulatorHelper helper;

	public override void Awake ()
	{
		base.Awake ();
		helper = new ShurikenManipulatorHelper(Target);
	}

	public override Color GetColor() {
		return helper.ParticleSystem.startColor;
		//return new Color ();
	}

	public override void SetColor(Color color) {
		helper.ParticleSystem.startColor = color;
	}
}
