using UnityEngine;
using System.Collections;

public class MaterialColorManipulator : ColorManipulator {

	public string colorParameter = "_MainTint";

	public MaterialManipulatorSettings settings = new MaterialManipulatorSettings();

	private MaterialManipulatorHelper helper;

	public override void Awake ()
	{
		base.Awake ();
		helper = new MaterialManipulatorHelper(Target);
	}

	public override Color GetColor () {
		return helper.GetMaterial(settings.materialIndex).GetColor(colorParameter); 
	}

	public override void SetColor (Color color) {
		helper.GetMaterial(settings.materialIndex).SetColor(colorParameter, color); 
	}
		
}
