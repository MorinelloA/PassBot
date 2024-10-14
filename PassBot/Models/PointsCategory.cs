using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassBot.Models
{
    using System.ComponentModel.DataAnnotations;

    public enum PointCategory
    {
        AttendCall,
        HaveCallQuestionAnswered,
        AnswerPoll,
        AttendFeedbackMeeting,
        TakeSurvey,
        BetaSignup,
        BetaTesting
    }

}
