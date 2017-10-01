using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Geom;

namespace Sonosthesia
{
    public interface ITransAccessor<TRepresentation> where TRepresentation : class
    {
        Trans GetTrans(TRepresentation representation);
        void SetTrans(TRepresentation representation, Trans trans);
    }

    abstract public class BaseTransAccessor<TRepresentation> : MonoBehaviour, ITransAccessor<TRepresentation> where TRepresentation : class
    {
        abstract public Trans GetTrans(TRepresentation representation);
        abstract public void SetTrans(TRepresentation representation, Trans trans);
    }

    public class TransAccessor : BaseTransAccessor<GameObject>
    {
        public bool local = true;

        public override Trans GetTrans(GameObject representation)
        {
            Transform t = GetTransform(representation);

            return local ? Trans.GetLocal(t) : Trans.GetGlobal(t);
        }

        public override void SetTrans(GameObject representation, Trans trans)
        {
            Transform t = GetTransform(representation);

            if (local)
            {
                trans.SetToLocal(t);
            }
            else
            {
                trans.SetToGlobal(t, true);
            }
        }

        protected virtual Transform GetTransform(GameObject representation)
        {
            return ComponentRegister.instance.GetTransform(representation);
        }
    }
}


