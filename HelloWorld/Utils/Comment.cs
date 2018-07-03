using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Protocol2.Utils
{
    [DataContract]
    public class Comment
    {
        [DataMember]
        public double left { get; set; }

        [DataMember]
        public double top { get; set; }

        [DataMember]
        public int width { get; set; }

        [DataMember]
        public int height { get; set; }

        [DataMember]
        public Color fill { get; set; }

        [DataMember]
        public double opacity { get; set; }

        [DataMember]
        public double angle { get; set; }

        public InkStrokeContainer ic { get; set; }


    }

    public class CommentUtils
    {
        private static List<Color> colorArray = new List<Color> {
            Colors.Goldenrod,
            Colors.LightSkyBlue,
            Colors.Plum,
            Colors.PaleGreen};

        private static Random rng = new Random();

        public static Comment CreateComment(double x, double y)
        {
            Comment comment = new Comment();

            comment.width = 25;
            comment.height = 25;
            comment.left = x - 12.5;
            comment.top = y - 12.5;
            comment.fill = colorArray[(rng.Next(0, colorArray.Count))];
            comment.angle = -30 + rng.Next(60);
            comment.opacity = 0.8;

            return comment;
        }
    }


}
