﻿using System;
using System.Net;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using PowerBIExtensionMethods;

namespace PBIGettingStarted {
    public class Dataset {
        private static JavaScriptSerializer Serializer { get; } = new JavaScriptSerializer();
        public string name { get; set; }
        public List<Table> tables { get; set; }
        public List<Relationship> relationships { get; set; }
        public string ToJson() => Serializer.Serialize(this);
    }

    public class Table {
        public string name { get; set; }
        public List<Column> columns { get; set; }
    }

    public class Column {
        public string name { get; set; }
        public string dataType { get; set; }
    }

    public class Relationship {
        public string name { get; set; }
        public string crossFilteringBehavior { get; set; } = "OneDirection";
        public string fromTable { get; set; }
        public string fromColumn { get; set; }
        public string toTable { get; set; }
        public string toColumn { get; set; }
    }

    public class AgeEntity {
        public int Id { get; set; }
        public int Age { get; set; }
    }

    public class NameEntity {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    class Program {
        private static readonly string ClientId = Properties.Resources.ClientId;
        //RedirectUri you used when you registered your app.
        //For a client app, a redirect uri gives AAD more details on the specific application that it will authenticate.
        private const string RedirectUri = "https://login.live.com/oauth20_desktop.srf";
        //Resource Uri for Power BI API
        private const string ResourceUri = "https://analysis.windows.net/powerbi/api";
        //OAuth2 authority Uri
        private const string Authority = "https://login.windows.net/common/oauth2/authorize";
        private static AuthenticationContext authContext;
        private static string token = string.Empty;

        //Uri for Power BI datasets
        private const string DatasetsUri = "https://api.powerbi.com/v1.0/myorg";

        //Example dataset name and group name
        private static readonly string DatasetName = "Data Set For Experiment Created at " + DateTime.Now;

        private static void Main() {
            CreateDataset();
            //AddRows("b7fd8eb0-f65c-4f83-83c6-74e19ba0e4d1", "Names");
            AddRows(
                "b7fd8eb0-f65c-4f83-83c6-74e19ba0e4d1",
                "Ages",
                new JavaScriptSerializer()
                    .Serialize(new List<AgeEntity> { new AgeEntity { Id = 42, Age = 33 } })
            );
        }

        //static void AddRows(string datasetId, string tableName) {
        //    //In a production application, use more specific exception handling. 
        //    try {
        //        var request = CreateRequestForDataset($"{DatasetsUri}/datasets/{datasetId}/tables/{tableName}/rows", "POST", AccessToken());
        //        var names = new List<NameEntity> {
        //            new NameEntity {Id = 12, Name = "Yue"},
        //            new NameEntity {Id = 42, Name = "Alan"}
        //        };
        //        var serializer = new JavaScriptSerializer();
        //        //POST request using the json from a list of Product
        //        //NOTE: Posting rows to a model that is not created through the Power BI API is not currently supported. 
        //        //      Please create a dataset by posting it through the API following the instructions on http://dev.powerbi.com.
        //        Console.WriteLine(PostRequest(request, serializer.Serialize(names)));
        //    } catch (Exception ex) {
        //        Console.WriteLine(ex.Message);
        //    }
        //}


        private static void CreateDataset() {
            var ds = new Dataset {
                name = DatasetName,
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
                        toTable = "Names",  //primary
                        toColumn = "Id",
                        crossFilteringBehavior = "BothDirections"
                    }
                }
            };

            try {
                var request = CreateRequestForDataset($"{DatasetsUri}/datasets", "POST", AccessToken());
                Console.WriteLine(PostRequest(request, ds.ToJson()));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }


        static void AddRows(string datasetId, string tableName, string json) {
            try {
                var request = CreateRequestForDataset($"{DatasetsUri}/datasets/{datasetId}/tables/{tableName}/rows", "POST", AccessToken());
                Console.WriteLine(PostRequest(request, json));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Use AuthenticationContext to get an access token
        /// </summary>
        /// <returns></returns>
        private static string AccessToken() {
            if (token == string.Empty) {
                //Get Azure access token
                // Create an instance of TokenCache to cache the access token
                var tc = new TokenCache();
                // Create an instance of AuthenticationContext to acquire an Azure access token
                authContext = new AuthenticationContext(Authority, tc);
                // Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
                token = authContext.AcquireToken(ResourceUri, ClientId, new Uri(RedirectUri), PromptBehavior.RefreshSession).AccessToken;
            } else {
                // Get the token in the cache
                token = authContext.AcquireTokenSilent(ResourceUri, ClientId).AccessToken;
            }

            return token;
        }
        private static string PostRequest(HttpWebRequest request, string json) {
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;

            //Write JSON byte[] into a Stream
            using (var writer = request.GetRequestStream()) {
                writer.Write(byteArray, 0, byteArray.Length);
            }

            return GetResponse(request);
        }

        private static string GetResponse(HttpWebRequest request) {
            string response;

            using (var httpResponse = request.GetResponse() as HttpWebResponse) {
                //Get StreamReader that holds the response stream
                using (var reader = new StreamReader(httpResponse.GetResponseStream())) {
                    response = reader.ReadToEnd();
                }
            }

            return response;
        }

        private static HttpWebRequest CreateRequestForDataset(string datasetsUri, string method, string accessToken) {
            var request = WebRequest.Create(datasetsUri) as HttpWebRequest;
            request.KeepAlive = true;
            request.Method = method;
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            return request;
        }
    }
}
