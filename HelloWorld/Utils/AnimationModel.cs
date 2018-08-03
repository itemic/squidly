using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;
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
        public Point startPoint {get; set;}
        public Point endPoint { get; set; }
        private double time { get; set; }

        public Rectangle animationRepresentation;


        public static int counter = 0; // temporary use
        public Animation()
        {
            inkStrokes = new List<InkStroke>();
            name = "Animation " + counter;
            id = counter;
            counter++;
            time = 1; //default animations are 2s
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
            startPoint = p.Points[0];
            endPoint = p.Points[p.Points.Count - 1];
            
        }

        public void setTime(double d)
        {
            time = d;
        }

        public double getInterval()
        {
            return time / polyline.Points.Count();
        }

        public Rectangle getRepresentation()
        {
            return animationRepresentation;
        }

        
        
    }

    public class AnimationModel
    {

      
        private ObservableCollection<Animation> animations { get; set; }
        public AnimationModel()
        {
            animations = new ObservableCollection<Animation>();
 
        }
        public void Add(Animation animation)
        {
            animations.Add(animation);
        }
        public ObservableCollection<Animation> GetAnimations()
        {
            //Reorder();
            foreach (var a in animations)
            {
                Debug.WriteLine(a.name);
            }
            return animations;
        }
        public void Reorder()
        {
            animations = new ObservableCollection<Animation>(animations.OrderBy(x => x.id).ToList());
        }

        public Animation Play(int id)
        {
            Animation anim = animations.Single(x => x.id == id);
            return anim;
        }
        public void RemoveAnimation(int id)
        {
            Animation anim = animations.Single(x => x.id == id);
            animations.Remove(anim);
        }

    }
}
