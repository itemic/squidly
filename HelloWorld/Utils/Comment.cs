using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace HelloWorld.Utils
{
    [DataContract]
    class Comment
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
        public SolidColorBrush fill { get; set; }

        [DataMember]
        public int angle { get; set; }


    }
}
