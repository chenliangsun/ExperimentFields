using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RestSharp;

namespace TrustableEmailClient
{
    public class EmailClient
    {
        // to write log file
        private static TextWriter writer; 

        /// <summary>
        /// entry point
        /// </summary>
        public static void Main()
        {
            string userInput = string.Empty;
            string logfile = ConfigurationManager.AppSettings["LogFilePath"];
            logfile += DateTime.UtcNow.ToString("yyyyMMddHHmmssff")+".log";

            writer = File.CreateText(logfile);

            // Get Email Service Provider (ESP)
            ArrayList emailServices = ReadEmailSerivceProviderFromConfig();

            while (true)
            {
                // Process user input
                Console.WriteLine("Enter email payload you want to send. Q to exit:");

                userInput = Console.ReadLine();
                if (userInput.Length == 0)
                {
                    Console.WriteLine("No input.");
                    return;
                }
                else if(userInput.ToLower() == "q")
                {
                    break;
                }

                OneEmail oneEmail = EmailPayloadParser(userInput);
                if (oneEmail == null)
                {
                    Console.WriteLine("email input validation failed. Please double check your input {0}", userInput);
                    return;
                }

                // Send email by invoking ESP http service. If one failed, try next until exhaust all ESP. 
                // ESP should be sorted by their performance, like reliability, scalability, fault tolerance, cost, etc.
                foreach (EmailClientSetting ecs in emailServices)
                {
                    try
                    {
                        if (ecs.IsRestSharp)
                        {
                            IRestResponse response = SendMessageUsingRestRequest(ecs, oneEmail);
                            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            {
                                // Console.WriteLine("Using {0} to send email failed with error {1}. Try next one", ecs.Endpoint, response.ErrorException);
                                EmailClientUtilities.Log(string.Format("Sending email from {0} failed with provider {1}", oneEmail.FromName, ecs.Endpoint), writer);
                                EmailClientUtilities.Log(string.Format("Sending email failed with error {0}", response.ErrorException), writer);
                            }
                            else
                            {
                                Console.WriteLine("Message was sent.");
                                // Console.WriteLine(response.Content);
                                EmailClientUtilities.Log(string.Format("Message from {0} was sent using {1}", oneEmail.FromName, ecs.Endpoint), writer);
                                EmailClientUtilities.Log(string.Format("Message was sent with response {0}", response.Content), writer);
                                break;
                            }
                        }
                        else
                        {
                            string response = SendMessageUsingWebClient(ecs, oneEmail);
                            if (string.IsNullOrEmpty(response))
                            {
                                // Console.WriteLine("Using {0} to send email failed. Try next one", ecs.Endpoint);
                                EmailClientUtilities.Log(string.Format("Using {0} to send email from {1} failed.", ecs.Endpoint, oneEmail.FromName), writer);
                            }
                            else
                            {
                                // idealy we should verify the sent status with the response. Like invoke ServiceProvider api 
                                // to get delivery status. Omitted here for simplicity
                                Console.WriteLine("Message was sent.");
                                //Console.WriteLine(response);
                                EmailClientUtilities.Log(string.Format("Message from {0} was sent using {1}", oneEmail.FromName, ecs.Endpoint), writer);
                                EmailClientUtilities.Log(string.Format("Message was sent with response {0}", response), writer);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.StackTrace);
                        EmailClientUtilities.Log(string.Format("Sending email failed with StackTrace {0}.", ex.StackTrace), writer);
                    }                    
                }
            }//while true

            writer.Flush();
            writer.Close();
        }

        /// <summary>
        /// read app.config
        /// </summary>
        /// <returns>configs in ArrayList</returns>
        public static ArrayList ReadEmailSerivceProviderFromConfig()
        {
            ArrayList emailServices = new ArrayList();

            // Get the application configuration file.
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            foreach (ConfigurationSection section in config.Sections)
            {
                if (!section.SectionInformation.SectionName.Contains("EmailClient")) 
                    continue;

                NameValueCollection sectionSettings = EmailClientUtilities.GetNameValueCollectionBySection(section, section.SectionInformation.SectionName);

                string userName = sectionSettings["UserName"];
                string apiKey = sectionSettings["ApiKey"];
                string endpoint = sectionSettings["EmailEndpoint"];
                bool isRestSharp = false;
                Boolean.TryParse(sectionSettings["IsRestSharp"], out isRestSharp);
                string domain = sectionSettings["Domain"];

                EmailClientSetting ecs = new EmailClientSetting(userName, apiKey, endpoint, isRestSharp, domain);
                emailServices.Add(ecs);
            }
            EmailClientUtilities.Log("Reading email service provider from config succeeded.", writer);
            return emailServices;
        }

        /*
        * Example Request Payload: 
        * {      "to": "fake@example.com",   
        * "to_name": "Ms. Fake",     
        * "from": "noreply@fake.com",    
        * "from_name": "Me",     
        * "subject": "A Message from Service provider",  
        * "body": "<h1>Your Bill</h1><p>$10</p>"  }  
        */
        /// <summary>
        /// create email payload
        /// </summary>
        /// <param name="payload">input json email string</param>
        /// <returns>oneEmail object</returns>
        public static OneEmail EmailPayloadParser(string payload)
        {
            Dictionary<string, string> jasonPair = new Dictionary<string, string>();

            ///process input
            payload = payload.Replace("\"", string.Empty).Trim(new char[] { '{', '}', ' ' });            
            string[] pairs = payload.Split(',');
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split(':');
                jasonPair.Add(parts[0].Trim().ToLower(), parts[1].Trim().ToLower());
            }

            string to = jasonPair["to"];
            string toName = jasonPair["to_name"];
            string from = jasonPair["from"];
            string fromName = jasonPair["from_name"];
            string subject = jasonPair["subject"];
            string body = EmailClientUtilities.StripHTML(jasonPair["body"]);

            //validate 
            if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(toName) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(fromName) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
                return null;

            OneEmail onemail = new OneEmail(from, fromName, to, toName, subject, body);

            return onemail;
        }  

        /// <summary>
        /// Send email using RestSharp request
        /// </summary>
        /// <param name="ecs">email service</param>
        /// <param name="email">email payload</param>
        /// <returns>rest response</returns>
        public static IRestResponse SendMessageUsingRestRequest(EmailClientSetting ecs, OneEmail email)
        {
            RestClient client = new RestClient();
            
            client.BaseUrl = ecs.Endpoint;
            client.Authenticator = new HttpBasicAuthenticator("api",ecs.ApiKey);
                                                
            RestRequest request = new RestRequest();
            request.AddParameter("domain", ecs.Domain, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", email.FromName + '<' +email.From +'>');
            request.AddParameter("to", email.To);            
            request.AddParameter("subject", email.Subject);
            request.AddParameter("html", email.Body);
            request.Method = Method.POST;

            return client.Execute(request);
        }     

        /// <summary>
        /// send email using normal WebClient
        /// </summary>
        /// <param name="ecs">email client setting</param>
        /// <param name="oneEmail">email payload</param>
        /// <returns>response string</returns>
        public static string SendMessageUsingWebClient(EmailClientSetting ecs, OneEmail oneEmail)
        {
            WebClient client = new WebClient();
            
            NameValueCollection values = new NameValueCollection();
            values.Add("username", ecs.UserName);
            values.Add("api_key", ecs.ApiKey);
            values.Add("from", oneEmail.From);
            values.Add("from_name", oneEmail.FromName);
            values.Add("subject", oneEmail.Subject);
            values.Add("body_text", oneEmail.Body); 
            values.Add("to", oneEmail.To);
           
            byte[] response = client.UploadValues(ecs.Endpoint, values); // POST by default
            return Encoding.UTF8.GetString(response);
        }         
    }//class
}//namespace
