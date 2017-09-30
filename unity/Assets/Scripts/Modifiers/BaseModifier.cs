using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{

    abstract public class BaseModifier<TRepresentation> : MonoBehaviour, IChannelModifier<TRepresentation> where TRepresentation : class
    {

        abstract public void ApplyParameter(TRepresentation representation, string key, IList<float> parameter);

        public virtual IEnumerable<ChannelParameterDescription> ParameterDescriptions
        {
            get
            {
                return Enumerable.Empty<ChannelParameterDescription>();
            }
        }
    }

}


