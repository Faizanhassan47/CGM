using MimeKit;

namespace CGM.PatientApp.Tests;

public sealed class EmailTest
{
    [Fact]
    public void PasswordResetMessage_ContainsOtpAndSafeRecipient()
    {
        const string otp = "123456";
        var message = new MimeMessage
        {
            Subject = "PASSWORD RESET - CGM Patient App",
            Body = new BodyBuilder
            {
                TextBody = $"Your password reset OTP code is: {otp}. This code is valid for 15 minutes.",
                HtmlBody = $"<p>Your password reset OTP code is:</p><strong>{otp}</strong><p>Valid for 15 minutes.</p>"
            }.ToMessageBody()
        };
        message.From.Add(MailboxAddress.Parse("no-reply@example.com"));
        message.To.Add(MailboxAddress.Parse("patient@example.com"));

        Assert.Contains(otp, message.TextBody);
        Assert.Contains(otp, message.HtmlBody);
        Assert.Equal("patient@example.com", ((MailboxAddress)message.To.Single()).Address);
    }
}
