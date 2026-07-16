using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class CommentShare
{
    public int CommentShareId { get; set; }

    public int CommentId { get; set; }

    public int UserId { get; set; }

    public DateTime SharedDate { get; set; }

    public virtual Comment Comment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
