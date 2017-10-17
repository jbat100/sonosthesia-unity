using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Sonosthesia
{
    public class TouchRayChannelOutput : RayChannelOutput
    {

        protected override Vector3? GetScreenPosition(int contactId)
        {
            Touch? touch = InteractionHelpers.GetTouchWithId(contactId);

            if (touch == null)
            {
                return null;
            }

            return touch.Value.position;
        }
        

        protected override void GetStartingContacts(List<int> list)
        {
            for (int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);

                Vector3? nullablePosition = GetScreenPosition(touch.fingerId);

                if (nullablePosition == null)
                {
                    continue;
                }

                RaycastHit? nullableHit = Raycast(nullablePosition.Value);

                if (touch.phase == TouchPhase.Began && nullableHit != null)
                {
                    list.Add(touch.fingerId);
                }
            }
        }

        protected override void GetEndingContacts(List<int> list)
        {
            for (int i = 0; i < Input.touchCount; ++i)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    list.Add(touch.fingerId);
                }
            }
        }
    }
}
