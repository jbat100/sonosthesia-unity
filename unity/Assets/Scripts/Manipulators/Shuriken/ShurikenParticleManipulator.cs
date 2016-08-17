using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShurikenParticleManipulator : ParticleManipulator {


	/*
	 * 
	 * It seems we have very little access to the ParticleSystem modules via scripting which seriously sucks 
	 * http://forum.unity3d.com/threads/access-to-particlesystem-internals-shuriken-from-script.261061/
	 * The base manipulator is used to access non-module attributes of the particle system. Hopefully the
	 * situation improves in future versions of Unity
	 * 
	 * use ParticleSystem.Emit directly in order to override colors
	 *  http://docs.unity3d.com/ScriptReference/ParticleSystem.Emit.html
	 *  http://docs.unity3d.com/ScriptReference/ParticleSystem.EmitParams.html
	 *  loop over the particles to alter their properties, for example based on lifetime
	 *  http://docs.unity3d.com/ScriptReference/ParticleSystem.Particle.html
	 * ParticleSystem particleSystem = GetTargetParticleSystem(target);
	 * var emission = particleSystem.emission;
	 * emission.rate = new ParticleSystem.MinMaxCurve( (r * (unitRate - zeroRate)) + zeroRate );
	 * 
	 */

	private ShurikenManipulatorHelper helper;

	public override void Awake ()
	{
		base.Awake ();
		helper = new ShurikenManipulatorHelper(Target);
	}

	public override void SetVelocity(float velocity) {
		helper.ParticleSystem.startSpeed = velocity;
	}

	public override void SetSize(float size) {
		helper.ParticleSystem.startSize = size;

	}
			
};
