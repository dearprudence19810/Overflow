using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
    public record QuestionUpdated(string QuestionId, string Title, string Content, List<string> Tags );
}
