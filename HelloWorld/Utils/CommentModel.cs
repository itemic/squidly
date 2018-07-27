using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Protocol2.Utils
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



        public Comment CreateComment(double x, double y, Color color)
        {
            Comment comment = new Comment();

            comment.width = 50;
            comment.height = 50;
            comment.left = x - 12.5;
            comment.top = y - 12.5;
            comment.fill = color;
            comment.angle = -20 + rng.Next(40);
            comment.opacity = 0.75;

            comments.Add(comment);

            return comment;
        }


        public List<Comment> GetComments()
        {
            return comments;
        }
    }
}
