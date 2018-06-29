using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace HelloWorld.Utils
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
        public double angle { get; set; }


    }
}
