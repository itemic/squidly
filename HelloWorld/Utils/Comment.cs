using System.Runtime.Serialization;
using Windows.UI;
using Windows.UI.Input.Inking;

namespace Squidly.Utils
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

   


}
