using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Squidly.Utils
{
    [DataContract]
    public class Animation: INotifyPropertyChanged
    {
        // public is only temporary!
        public List<InkStroke> inkStrokes { get; set; }

        [DataMember]
        public List<int> inkStrokesIndex { get; set; }

        // this is going to be used until we save, at which point we will use the index
        [DataMember]
        public List<uint> inkStrokesId { get; set; }

        public Polyline polyline { get; set; }
        public TextBlock nameText { get; set; }

        [DataMember]
        private string name;
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public Point startPoint { get; set; }
        [DataMember]
        public Point endPoint { get; set; }
        [DataMember]
        private double time { get; set; }
        [DataMember]
        public PointCollection linePoints { get; set; }
        [DataMember]
        public int length { get; set; } //just number of points in the polyline
        [DataMember]
        public double position { get; set; } //position in canvas - directly proportional to time the animation runs

        [DataMember]
        public static int counter = 0; // temporary use

        public event PropertyChangedEventHandler PropertyChanged;

        private bool isEnabled;

        public bool isActive;
    

        public Animation()
        {
            inkStrokes = new List<InkStroke>();
            inkStrokesIndex = new List<int>();
            inkStrokesId = new List<uint>();
            nameText = new TextBlock();


            name = name == null || name == "" ? "Animation " + counter : name;
            //SetName(name);

            id = counter;
            counter++;
            time = 1; //default animations are 2s
            isEnabled = true;
        }

        public Polyline GetPolyline()
        {
            return polyline;
        }

        public void SetPolyline(Polyline p)
        {
            polyline = p;
            startPoint = p.Points[0];
            endPoint = p.Points[p.Points.Count - 1];
            linePoints = p.Points;

            length = polyline.Points.Count;
        }

        public String Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {

            get { return this.isEnabled; }
            set
            {
                this.isEnabled = value;
                this.OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AnimationComparer : IComparer<Animation>
    {
        public int Compare(Animation x, Animation y)
        {
            int result = x.position.CompareTo(y.position);
            if (result == 0)
            {
                return 1;
            } else
            {
                return result;
            }
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
            return animations;
        }

        public void Reorder()
        {
            animations = new ObservableCollection<Animation>(animations.OrderBy(x => x.id).ToList());
        }

        public Animation GetAnimationAt(int id)
        {
            Animation anim = animations.SingleOrDefault(x => x.id == id);
            return anim;
        }

        public void RemoveAnimation(int id)
        {
            Animation anim = animations.SingleOrDefault(x => x.id == id);

            animations.Remove(anim);
        }

        public void SetAnimationName(int id, String newName)
        {
            Animation animation = GetAnimationAt(id);
            animation.Name = newName;
        }



    }
}
