using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public List<Double> distArray;

        public static int counter = 0; // temporary use
        public Animation()
        {
            inkStrokes = new List<InkStroke>();
            name = "Animation " + counter;
            id = counter;
            counter++;
            distArray = new List<Double>();
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
            normalize();
        }

        public void normalize()
        {
            double d = 0.0;
            for (int i = 1; i < polyline.Points.Count(); i++)
            {
                var delx = polyline.Points[i].X - polyline.Points[i - 1].X;
                var dely = polyline.Points[i].Y - polyline.Points[i - 1].Y;
                var dist = Math.Sqrt(Math.Pow(delx, 2) + Math.Pow(dely, 2));
                Debug.WriteLine("dist: " + dist);
                distArray.Add(dist);
                d += dist;
            }

            for (int i = 0; i < distArray.Count(); i++)
            {
                distArray[i] /= d;
                Debug.WriteLine("normeld:" + distArray[i]);
            }

        }
        
    }

    public class AnimationModel
    {

      
        private ObservableCollection<Animation> animations { get; }
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
            return animations;
        }

    }
}
