using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.spacepuppy.Geom;

namespace Sonosthesia
{
    public interface IColorAccessor<TRepresentation> where TRepresentation : class
    {
        Color GetColor(TRepresentation representation);
        void SetColor(TRepresentation representation, Color color);
    }

    public interface ITransAccessor<TRepresentation> where TRepresentation : class
    {
        Trans GetTrans(TRepresentation representation);
        void SetColor(TRepresentation representation, Trans trans);
    }

}