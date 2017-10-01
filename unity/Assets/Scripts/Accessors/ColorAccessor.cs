using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    public interface IColorAccessor<TRepresentation> where TRepresentation : class
    {
        Color GetColor(TRepresentation representation);
        void SetColor(TRepresentation representation, Color color);
    }

    abstract public class BaseColorAccessor<TRepresentation> : MonoBehaviour, IColorAccessor<TRepresentation> where TRepresentation : class
    {
        abstract public Color GetColor(TRepresentation representation);
        abstract public void SetColor(TRepresentation representation, Color color);
    }

    public class ColorAccessor : BaseColorAccessor<GameObject>
    {
        public int materialIndex = 0;

        public string propertyName;

        public override Color GetColor(GameObject representation)
        {
            Material material = GetMaterial(representation);

            if (material)
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    return material.GetColor(propertyName);
                }
                return material.color;
            }

            return default(Color);
        }

        public override void SetColor(GameObject representation, Color color)
        {
            Material material = GetMaterial(representation);

            if (material)
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    material.SetColor(propertyName, color);
                }
                material.color = color;
            }
        }

        protected virtual Material GetMaterial(GameObject representation)
        {
            Renderer renderer = ComponentRegister.instance.GetRenderer(representation);
            if (renderer)
            {
                return (materialIndex > 0 && renderer.materials.Length > materialIndex) ? renderer.materials[materialIndex] : renderer.material;
            }
            return null;
        }
    }

}

