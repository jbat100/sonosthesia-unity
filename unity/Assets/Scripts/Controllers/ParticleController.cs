using UnityEngine;
using System.Collections;



namespace Sonosthesia.Obsolete
{
    public class ParticleController : SonosthesiaController
    {

        public FloatMapping velocityMapping = new FloatMapping(0.1f, 5.0f);
        public FloatMapping sizeMapping = new FloatMapping(0.1f, 1.9f);

        // Use this for initialization
        public override void Start()
        {

            base.Start();

            controlDelegates["size"] = delegate (ControlTarget target, ArrayList values) {
                ParticleManipulator manipulator = target.GetManipulator<ParticleManipulator>();
                manipulator.SetSize(sizeMapping.Map((float)(values[0])));
            };

            controlDelegates["velocity"] = delegate (ControlTarget target, ArrayList values) {
                ParticleManipulator manipulator = target.GetManipulator<ParticleManipulator>();
                manipulator.SetVelocity(velocityMapping.Map((float)(values[0])));
            };


        }

    }

}
