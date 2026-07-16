using System;
using System.Collections.Generic;

namespace quizportal.Models;

public partial class UserFavouriteQuestion
{
    public int UserId { get; set; }

    public int QuestionId { get; set; }

    public DateTime MarkedDate { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
