//********************************************************* 
//
//    This code has been created by Amy Nicholson
//    Technical Evangelist, Microsoft
//    This code is for personal demo purposes only
//    without warrenty of any kind
// 
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure;
using System.Net.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json;

namespace DisplayAvgSentiment
{
    class Program
    {
        class SentimentData
        {
            public int AverageSentiment { get; set; }
            public string EventHubName { get; set; }
        }


        static void Main(string[] args)
        {
            string ehName = "<EVENTHUB_NAME>";
            string connection = "Endpoint=sb://<SERVICEBUS_NAMESPACE>.servicebus.windows.net/;SharedAccessKeyName=<ACCESS_POLICY_NAME>;SharedAccessKey=<ACCESSKEY>;TransportType=Amqp";
            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(connection);
            EventHubClient ehub = factory.CreateEventHubClient(ehName);
            EventHubConsumerGroup group = ehub.GetDefaultConsumerGroup();
            EventHubReceiver reciever = group.CreateReceiver("0");


            while (true)
            {
                EventData data = reciever.Receive();
                if (data != null)
                {
                    try
                    {
                        string message = Encoding.UTF8.GetString(data.GetBytes());
                        string value = message.Substring(16, 4);
                        var avg = Convert.ToDouble(value);
                        var integerValue = Convert.ToInt16(avg);

                        Console.WriteLine(message);
                        Console.WriteLine(integerValue);

                        //create sentimentdata object
                        var sentimentData = new SentimentData() { AverageSentiment = integerValue, EventHubName = ehName };

                        //post sentimentdata to api
                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri("http://reasonsapi.azurewebsites.net");
                            var response = client.PostAsJsonAsync("/api/sentimentdata", sentimentData).Result;
                            //Console.WriteLine(response.StatusCode); //this line is not required but there for debug purposes
                        }

                        //Console.ReadKey(); //this line is not required but there for debug purposes
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }


        }
    }
}