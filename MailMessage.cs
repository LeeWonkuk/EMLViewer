using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using DevExpress.XtraEditors.TextEditController.IME;
using MimeKit;

namespace EmlViewer
{
    public class MailMessage
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Date { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Attachment { get; set; }
        public List<MimePart> Attachments { get; set; }

        public void Load(StreamReader reader)
        {
            try
            {
                var message = MimeMessage.Load(reader.BaseStream);

                Subject = message.Subject;
                Body = message.TextBody;
                Date = message.Date.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                From = string.Join(";", message.From.Mailboxes).Replace("\"", "");
                To = string.Join(";", message.To.Mailboxes).Replace("\"", "");
                Cc = string.Join(";", message.Cc.Mailboxes).Replace("\"", "");

                Attachments = new List<MimePart>();
                foreach (var attachment in message.Attachments)
                {
                    // Check if the attachment is a MimePart (can be saved)
                    if (attachment is MimePart)
                    {
                        var part = (MimePart)attachment;
                        Attachments.Add(part);
                    }
                }
                if (Attachments.Count > 0)
                {
                    Attachment = "@";
                }
            }
            catch (Exception)
            {
            }
        }
    }
}