using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustableEmailClient
{
     public class OneEmail
    {
        public string To
        {
            get;
            set;
        }

        public string ToName
        {
            get;
            set;
        }

        public string From
        {
            get;
            set;
        }

        public string FromName
        {
            get;
            set;
        }

        public string Subject
        {
            get;
            set; 
        }

        public string Body
        {
            get;
            set;
        }

        public OneEmail(string from, string fromName, string to, string toName, string subject, string body)
        {
            this.From = from;
            this.FromName = fromName;
            this.To = to;
            this.ToName = toName;
            this.Subject = subject;
            this.Body = body;
        }
    }
}
