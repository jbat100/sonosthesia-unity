﻿using UnityEngine;

namespace com.spacepuppy
{

    /// <summary>
    /// Base contract for any interface contract that should be considered a Component
    /// </summary>
    public interface IComponent : IGameObjectSource
    {

        bool enabled { get; set; }
        bool isActiveAndEnabled { get; }
        Component component { get; }

    }

}
