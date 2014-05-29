Short Introduction: 

A simple email client application that uses email service providers' HTTP service to send email. Email services are configured on the backend in the order of email service performance like reliability, response time, scalability, cost, etc. If one service failed, it will automatically fail over t next one hence decrease the service provider outage.

Usage:

TrustableEmailClient.exe
Enter email payload you want to send. Q to exit:
{      to: chsun@microsoft.com, to_name: Ms. Fake, from: noreply@fake.com, from_name: Me,subject: A Message from Service provider, body: <h1>Your Bill</h1><p>$1
0</p> }


Features:
1. Using widely acceppted HTTP service instead of SMTP service which may be blocked by firewall.
2. This service service provider list is configureable. You can simply change the config file to add/remove/edit email services.
3. Simple code. Easy to deploy and maintainance.

Deployment:
Just copy RestSharp.dll, RestSharp.xml (C# package), TrustableEmailClient.exe, TrustableEmailClient.exe.config to your local machine and you are good to go.

Troubleshooting:
There are detailed log file in the same place (configureable) with naming format EmailingRecordYYYYMMddHHmmssff.log to view execution history like when email was sent, what response code, response content, etc. 

Discussion:
1. Input ideally a Web UI. That needs a Web site to host the Emailing service which is out of the scope of this project.
2. We can also choose to log to database. Log to file is simple but difficult to query each field like status, return code, error messages. 
3. Using C# since I am most familiar with it and Visual Studio is one of the most powerfull IDE.
4. Didn't use mandrill since it doesn't have API for C#.
