//********************************************************* 
// 
//    Copyright (c) Microsoft. All rights reserved. 
//    This code is licensed under the Microsoft Public License. 
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF 
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY 
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR 
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT. 
//
//    This code has been edited by Amy Nicholson
//    Technical Evangelist, Microsoft
//    This code is for demo purposes only
// 
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Newtonsoft.Json;

namespace TwitterClient
{
    class Program
    {

        public static int SentimentValue(string inputText, HttpClient httpClient)
        {
            // get sentiment
            string inputTextEncoded = HttpUtility.UrlEncode(inputText);
            string sentimentRequest = "data.ashx/amla/text-analytics/v1/GetSentiment?Text=" + inputTextEncoded;
            var responseTask = httpClient.GetAsync(sentimentRequest);
            responseTask.Wait();
            var response = responseTask.Result;
            var contentTask = response.Content.ReadAsStringAsync();
            contentTask.Wait();
            var content = contentTask.Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Call to get sentiment failed with HTTP status code: " +
                                    response.StatusCode + " and contents: " + content);
            }

            SentimentResult sentimentResult = JsonConvert.DeserializeObject<SentimentResult>(content);
            Console.WriteLine("Sentiment score: " + sentimentResult.Score);
            Console.WriteLine("Tweet: " + inputText);
            //IF statement to check for 0 Exception
            return (int)(sentimentResult.Score * 100);
        }

        static void Main(string[] args)
        {
            //Configure Twitter OAuth
            var oauthToken = ConfigurationManager.AppSettings["oauth_token"];
            var oauthTokenSecret = ConfigurationManager.AppSettings["oauth_token_secret"];
            var oauthCustomerKey = ConfigurationManager.AppSettings["oauth_consumer_key"];
            var oauthConsumerSecret = ConfigurationManager.AppSettings["oauth_consumer_secret"];
            var keywords = ConfigurationManager.AppSettings["twitter_keywords"];
            var accountKey = ConfigurationManager.AppSettings["accountkey"];

            //Configure EventHub
            var config = new EventHubConfig();
            config.ConnectionString = ConfigurationManager.AppSettings["EventHubConnectionString"];
            config.EventHubName = ConfigurationManager.AppSettings["EventHubName"];
            var myEventHubObserver = new EventHubObserver(config);

            //Cognitive Service call
            string ServiceBaseUri = "https://api.datamarket.azure.com/";
            var httpClient = new HttpClient();
            
            httpClient.BaseAddress = new Uri(ServiceBaseUri);
            string creds = "AccountKey:" + accountKey;
            string authorizationHeader = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(creds));
            httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            //Tweet Response
            var datum = Tweet.StreamStatuses(new TwitterConfig(oauthToken, oauthTokenSecret, oauthCustomerKey, oauthConsumerSecret,
            keywords)).Select(tweet => Sentiment.ComputeScore(tweet, keywords)).Select(tweet => new Payload { CreatedAt=Convert.ToDateTime(tweet.CreatedAt),Topic = tweet.Topic, SentimentScore= SentimentValue(tweet.Text,httpClient), Text = tweet.Text});

            datum.ToObservable().Subscribe(myEventHubObserver);


        }

        /// <summary>
        /// Class to hold result of Sentiment call
        /// </summary>
        public class SentimentResult
        {
            public double Score { get; set; }
        }
    }
}
