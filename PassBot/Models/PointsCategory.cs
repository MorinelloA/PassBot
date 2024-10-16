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
