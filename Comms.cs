using System.Net.Mail;
dbffoxpnzvlfwdnhjccyoh6nbo2bvnwz2jaa7g3vk3e2v2dqa5zq
public static class Comms
{
    public static void SendEmail(string body, string subject)
    {
        string from = ConfigurationHelper.GetByName("MailSettings:From");
        string to = ConfigurationHelper.GetByName("MailSettings:To");
        string password = ConfigurationHelper.GetByName("MailSettings:Password");
        SmtpClient client = new SmtpClient(ConfigurationHelper.GetByName("MailSettings:Host"));
        client.Port = int.Parse(ConfigurationHelper.GetByName("MailSettings:Port"));
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.UseDefaultCredentials = false;
        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(from, password);
        client.EnableSsl = true;
        client.Credentials = credentials;
        MailMessage message = new MailMessage(from, to);
        message.IsBodyHtml = true;
        message.Subject = subject;
        message.Body = body;
        client.Send(message);
    }
}
