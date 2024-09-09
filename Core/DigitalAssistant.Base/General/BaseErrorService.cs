namespace DigitalAssistant.Base.General;

public class BaseErrorService
{
    public string PrepareExceptionErrorMessage(Exception e)
    {
        var message = e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace;
        if (e.InnerException == null)
            return message;

        return message + Environment.NewLine + Environment.NewLine + "Inner Exception:" + PrepareExceptionErrorMessage(e.InnerException);
    }
}
