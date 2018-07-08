using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace HelloWorld
{
    class StrokeGroup
    {
        private List<InkStroke> strokes; 

        public StrokeGroup()
        {
            strokes = new List<InkStroke>();
        }

        public void AddStroke(InkStroke stroke)
        {
            if (!strokes.Contains(stroke))
            strokes.Add(stroke);
        }

        public void selectStrokesInGroup()
        {
            foreach (var stroke in strokes)
            {
                stroke.Selected = true;
            } 
        }

        public Rect findBoundingBox()
        {
            var outermostLeftX = strokes[0].BoundingRect.X;
            var outermostRightX = strokes[0].BoundingRect.X + strokes[0].BoundingRect.Width;
            var outermostTopY = strokes[0].BoundingRect.Y;
            var outermostBottomY = strokes[0].BoundingRect.Y - strokes[0].BoundingRect.Height;
            foreach (var stroke in strokes)
            {
                Rect boundingRect = stroke.BoundingRect;

                var leftX = boundingRect.X;
                var rightX = boundingRect.X + boundingRect.Width;
                var topY = boundingRect.Y;
                var bottomY = boundingRect.Y + boundingRect.Height;

                if (leftX < outermostLeftX)
                {
                    outermostLeftX = leftX;
                }

                if (rightX > outermostRightX)
                {
                    outermostRightX = rightX;
                }

                if (topY < outermostTopY)
                {
                    outermostTopY = topY;
                }

                if (bottomY > outermostBottomY)
                {
                    outermostBottomY = bottomY;
                }
            }

            return new Rect(outermostLeftX, outermostTopY, outermostRightX - outermostLeftX, outermostBottomY - outermostTopY);
        } 

        public ReadOnlyCollection<InkStroke> getStrokes()
        {
            return new ReadOnlyCollection<InkStroke>(strokes);
        }
    }
}
