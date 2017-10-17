using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sonosthesia
{
    
    public class MouseRayChannelOutput : RayChannelOutput
    {

        public bool useLeft = true;
        public bool useRight = true;
        public bool useMiddle = true;

        protected override Vector3? GetScreenPosition(int contactId)
        {
            return Input.mousePosition;
        }

        protected override void GetStartingContacts(List<int> list)
        {
            RaycastHit? hit = Raycast(Input.mousePosition);

            if (hit != null)
            {
                InteractionHelpers.GetMouseButtonDowns(list, useLeft, useRight, useMiddle);
            }
        }

        protected override void GetEndingContacts(List<int> list)
        {
            InteractionHelpers.GetMouseButtonUps(list, useLeft, useRight, useMiddle);
        }
    }

}
