using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Collections;

namespace Sonosthesia
{

    // caches components on game objects for fast retrieval, performance is critical when having many many channel instances
    public class ComponentCache<TComponent> where TComponent : Component
    {
        private Dictionary<int, TComponent> _components;

        // stop the cache from growing to silly length, maybe find a good way of recycling intelligently, but for now, just clears on 
        private int _size;
        
        public ComponentCache(int size = 1000)
        {
            _size = size;
            _components = new Dictionary<int, TComponent>();
        }

        public TComponent GetComponent(GameObject obj, bool autocreate = false)
        {
            TComponent result = null;

            // http://answers.unity3d.com/questions/1261424/getcomponent-alternative-for-getting-objects.html
            // http://naplandgames.com/blog/2016/10/05/diagnostics/

            if (!_components.TryGetValue(obj.GetInstanceID(), out result))
            {
                result = obj.GetComponent<TComponent>();
                if(result == null && autocreate)
                {
                    result = obj.AddComponent<TComponent>();
                }
                if (result)
                {
                    // TODO: find a better way to handle large cache...
                    if (_components.Count > _size)
                    {
                        _components.Clear();
                    }
                    _components[obj.GetInstanceID()] = result;
                }
            }

            return result;
        }
    }


    public class ComponentRegister
    {
        public static ComponentRegister instance { get { return _instance; } }

        private static ComponentRegister _instance = new ComponentRegister();

        private static int size = 1000;

        private ComponentCache<Renderer> _rendererCache;
        private ComponentCache<Rigidbody> _rigidbodyCache;
        private ComponentCache<Transform> _transformCache;

        public ComponentRegister()
        {
            _rendererCache = new ComponentCache<Renderer>(size);
            _rigidbodyCache = new ComponentCache<Rigidbody>(size);
            _transformCache = new ComponentCache<Transform>(size);
        }

        public Renderer GetRenderer(GameObject obj)
        {
            return _rendererCache.GetComponent(obj);
        }

        public Transform GetTransform(GameObject obj)
        {
            return _transformCache.GetComponent(obj);
        }

    }

}

