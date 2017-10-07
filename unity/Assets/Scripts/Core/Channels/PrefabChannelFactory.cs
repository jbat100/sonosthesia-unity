using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{

    public class PrefabChannelFactory : BaseChannelFactory
    {
        public GameObject prefab;

        public Transform instanceParent;

        protected override Func<GameObject> FactoryConstructor()
        {
            return () => { return Instantiate(prefab, instanceParent); };
        }
    }

}


