using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class CommentReaction
{
    public int CommentReactionId { get; set; }

    public int CommentId { get; set; }

    public int UserId { get; set; }

    public int ReactionType { get; set; }

    public DateTime ReactedDate { get; set; }

    public virtual Comment Comment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
