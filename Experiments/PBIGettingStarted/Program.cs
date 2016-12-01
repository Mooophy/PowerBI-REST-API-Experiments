using System;
using System.Net;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace PBIGettingStarted
{
    public class Dataset
    {
        private static JavaScriptSerializer Serializer { get; } = new JavaScriptSerializer();
        public string name { get; set; }
        public List<Table> tables { get; set; }
        public List<Relationship> relationships { get; set; }
        public string ToJson() => Serializer.Serialize(this);
    }

    public class Table
    {
        public string name { get; set; }
        public List<Column> columns { get; set; }
    }

    public class Column
    {
        public string name { get; set; }
        public string dataType { get; set; }
    }

    public class Relationship
    {
        public string name { get; set; }
        public string crossFilteringBehavior { get; set; } = "OneDirection";
        public string fromTable { get; set; }
        public string fromColumn { get; set; }
        public string toTable { get; set; }
        public string toColumn { get; set; }
    }

    class Program
    {
        private static readonly string ClientId = Properties.Resources.ClientId;
        //RedirectUri you used when you registered your app.
        //For a client app, a redirect uri gives AAD more details on the specific application that it will authenticate.
        private static string redirectUri = "https://login.live.com/oauth20_desktop.srf";
        //Resource Uri for Power BI API
        private static string resourceUri = "https://analysis.windows.net/powerbi/api";
        //OAuth2 authority Uri
        private static string authority = "https://login.windows.net/common/oauth2/authorize";
        private static AuthenticationContext authContext;
        private static string token = string.Empty;

        //Uri for Power BI datasets
        private static string datasetsUri = "https://api.powerbi.com/v1.0/myorg";

        //Example dataset name and group name
        private static string datasetName = "Data Set For Experiment Created at " + DateTime.Now.ToUniversalTime();

        static void Main(string[] args)
        {
            var serializer = new JavaScriptSerializer();
            var ds = new Dataset
            {
                name = datasetName,
                tables = new List<Table>
                    {
                        new Table
                        {
                            name = "Ages",
                            columns = new List<Column>
                            {
                                new Column
                                {
                                    name = "Id",
                                    dataType = "Int64"
                                },
                                new Column
                                {
                                    name = "Age",
                                    dataType = "Int64"
                                }
                            }
                        },
                        new Table
                        {
                            name = "Names",
                            columns = new List<Column>
                            {
                                new Column
                                {
                                    name = "Id",
                                    dataType = "Int64"
                                },
                                new Column
                                {
                                    name = "Name",
                                    dataType = "string"
                                }
                            }
                        },
                    },
                relationships = new List<Relationship>
                {
                    new Relationship
                    {
                        name = "IdToId",
                        fromTable = "Ages",
                        fromColumn = "Id",
                        toTable = "Names",
                        toColumn = "Id",
                        crossFilteringBehavior = "BothDirections"
                    }
                }
            };

            try
            {
                //Create a POST web request to list all datasets
                HttpWebRequest request = CreateRequestForDataset($"{datasetsUri}/datasets", "POST", AccessToken());
                //Console.WriteLine(PostRequest(request, new Product().ToDatasetJson(datasetName)));
                Console.WriteLine(PostRequest(request, ds.ToJson()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }

        /// <summary>
        /// Use AuthenticationContext to get an access token
        /// </summary>
        /// <returns></returns>
        static string AccessToken()
        {
            if (token == String.Empty)
            {
                //Get Azure access token
                // Create an instance of TokenCache to cache the access token
                TokenCache TC = new TokenCache();
                // Create an instance of AuthenticationContext to acquire an Azure access token
                authContext = new AuthenticationContext(authority, TC);
                // Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
                token = authContext.AcquireToken(resourceUri, ClientId, new Uri(redirectUri), PromptBehavior.RefreshSession).AccessToken;
            }
            else
            {
                // Get the token in the cache
                token = authContext.AcquireTokenSilent(resourceUri, ClientId).AccessToken;
            }

            return token;
        }
        private static string PostRequest(HttpWebRequest request, string json)
        {
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;

            //Write JSON byte[] into a Stream
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteArray, 0, byteArray.Length);
            }

            return GetResponse(request);
        }

        private static string GetResponse(HttpWebRequest request)
        {
            string response = string.Empty;

            using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
            {
                //Get StreamReader that holds the response stream
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    response = reader.ReadToEnd();
                }
            }

            return response;
        }

        private static HttpWebRequest CreateRequestForDataset(string datasetsUri, string method, string accessToken)
        {
            HttpWebRequest request = WebRequest.Create(datasetsUri) as HttpWebRequest;
            request.KeepAlive = true;
            request.Method = method;
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken));
            return request;
        }
    }
}
