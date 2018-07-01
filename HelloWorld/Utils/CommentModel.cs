using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld.Utils
{
    public class CommentModel
    {
        public List<Comment> comments;

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
    }
}
