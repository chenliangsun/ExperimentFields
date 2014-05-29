using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustableEmailClient
{
    public class EmailClientSetting
    {
        public string UserName
        {
            get;
            set;
        }

        public bool IsRestSharp
        {
            get;
            set;
        }

        public string ApiKey
        {
            get;
            set;
        }

        public string Endpoint
        {
            get;
            set;
        }

        public string Domain
        {
            get;
            set;
        }

        public EmailClientSetting(string userName, string apiKey, string endpoint, bool isrest, string domain)
        {
            this.UserName = userName;
            this.ApiKey = apiKey;
            this.Endpoint = endpoint;
            this.IsRestSharp = isrest;
            this.Domain = domain;
        }
    }
}
