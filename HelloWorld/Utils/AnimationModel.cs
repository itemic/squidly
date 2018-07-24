using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Shapes;

namespace Protocol2.Utils
{

    public class Animation
    {
        // public is only temporary!
        public List<InkStroke> inkStrokes { get; set; }
        public Polyline polyline { get; set; }
        public string name { get; set; }
        public int id { get; set; }
        public static int counter = 0; // temporary use
        public Animation()
        {
            inkStrokes = new List<InkStroke>();
            name = "Animation " + counter;
            id = counter;
            counter++;
        }

        public Polyline GetPolyline()
        {
            return polyline;
        }

        public List<InkStroke> GetInkStrokes()
        {
            return inkStrokes;
        }

        public void SetPolyline(Polyline p)
        {
            polyline = p;
        }
        
    }

    public class AnimationModel
    {

      
        private List<Animation> animations { get; }
        public AnimationModel()
        {
            animations = new List<Animation>();
        }
        public void Add(Animation animation)
        {
            animations.Add(animation);
        }
        public List<Animation> GetAnimations()
        {
            return animations;
        }

    }
}
