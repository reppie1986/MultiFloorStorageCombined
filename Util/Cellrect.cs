using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MultiFloorStorage.Util
{
    public static class CellRectExtensions
    {
        public static Rect ToScreenRect(this CellRect cellRect)
        {
            // FIX #1: CellRect doesn't have BottomLeft or TopRight properties.
            // We create them manually from min/max coordinates.
            IntVec3 bottomLeft = new IntVec3(cellRect.minX, 0, cellRect.minZ);
            IntVec3 topRight = new IntVec3(cellRect.maxX, 0, cellRect.maxZ);

            Vector3 bottomLeftWorld = bottomLeft.ToVector3Shifted();
            Vector3 topRightWorld = topRight.ToVector3Shifted();

            // FIX #2: The method is named MapToUIPosition, not WorldToUIPosition.
            Vector2 bottomLeftUI = bottomLeftWorld.MapToUIPosition();
            Vector2 topRightUI = topRightWorld.MapToUIPosition();

            // The rest of your logic for calculating width and height is correct.
            float width = topRightUI.x - bottomLeftUI.x;
            float height = bottomLeftUI.y - topRightUI.y; // Y coordinates are inverted on screen

            return new Rect(bottomLeftUI.x, topRightUI.y, width, height);
        }
    }
}