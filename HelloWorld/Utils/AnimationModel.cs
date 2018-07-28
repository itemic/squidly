﻿using System;
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
        private double time { get; set; }


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
            Debug.WriteLine(time / polyline.Points.Count());
            return time / polyline.Points.Count();
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
