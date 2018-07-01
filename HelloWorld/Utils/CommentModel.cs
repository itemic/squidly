using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace HelloWorld.Utils
{
    public class CommentModel
    {
        private List<Comment> comments;
        private Random rng = new Random();
        private List<Color> colorArray = new List<Color> {
            Colors.Goldenrod,
            Colors.LightSkyBlue,
            Colors.Plum,
            Colors.PaleGreen};

        public CommentModel()
        {
            comments = new List<Comment>();
        }

        internal void Add(Comment comment)
        {
            comments.Add(comment);
        }

        internal void Remove(Comment comment)
        {
            comments.Remove(comment);
        }

        public Comment CreateComment(double x, double y)
        {
            Comment comment = new Comment();

            comment.width = 25;
            comment.height = 25;
            comment.left = x - 12.5;
            comment.top = y - 12.5;
            comment.fill = colorArray[(rng.Next(0, colorArray.Count))];
            comment.angle = -30 + rng.Next(60);
            comment.opacity = 0.8;

            comments.Add(comment);

            return comment;
        }


        public List<Comment> GetComments()
        {
            return comments;
        }
    }
}
