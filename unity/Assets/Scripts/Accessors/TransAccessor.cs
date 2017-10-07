using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Geom;

namespace Sonosthesia
{
    public interface ITransAccessor
    {
        Trans GetTrans(GameObject representation);
        void SetTrans(GameObject representation, Trans trans);
    }

    abstract public class BaseTransAccessor : MonoBehaviour, ITransAccessor
    {
        abstract public Trans GetTrans(GameObject representation);
        abstract public void SetTrans(GameObject representation, Trans trans);
    }

    public class TransAccessor : BaseTransAccessor
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


