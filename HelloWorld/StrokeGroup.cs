using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public float findBoundingBox()
        {
            foreach (var stroke in strokes)
            {
                
            }
        } 

        public ReadOnlyCollection<InkStroke> getStrokes()
        {
            return new ReadOnlyCollection<InkStroke>(strokes);
        }
    }
}
