﻿namespace XenForo.NET
{
    using System;

    using Newtonsoft.Json.Linq;

    using RestSharp;
    using RestSharp.Authenticators;

    using XenForo.NET.Models.Api;
    using XenForo.NET.Models.Enums;

    public partial class XenforoApi
    {
        /// <summary>
        /// Gets the rest client.
        /// </summary>
        private RestClient Client
        {
            get;
        }
        
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        private XenForoConfig Config
        {
            get;
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        private Token Token
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XenforoApi"/> class.
        /// </summary>
        public XenforoApi(XenForoConfig Config)
        {
            if (Config == null)
            {
                throw new Exception("Config cannot be null at XenforoApi(Config)!");
            }

            this.Config = Config;
            this.Client = new RestClient(Config.Url);
        }

        /// <summary>
        /// Authenticates the user using the specified credentials.
        /// </summary>
        /// <param name="Username">The username.</param>
        /// <param name="Password">The password.</param>
        public void Authenticate(string Username, string Password)
        {
            var Request = new RestRequest("?oauth/token");

            Request.AddParameter("client_id", this.Config.ClientId);
            Request.AddParameter("client_secret", this.Config.ClientSecret);
            Request.AddParameter("grant_type", "password");

            Request.AddParameter("username", Username);
            Request.AddParameter("password", Password);

            var Response = this.Client.Post(Request);

            if (Response.IsSuccessful)
            {
                var Json = JObject.Parse(Response.Content);

                if (Json != null && Json.HasValues)
                {
                    if (!Json.ContainsKey("access_token"))
                    {
                        Logging.Warning(this.GetType(), "[*] Json.ContainsKey('access_token') != true at XenforoApi.Authenticate(Username, Password).");
                    }
                    else
                    {
                        if (Json.GetValue("token_type").ToObject<string>() == "Bearer")
                        {
                            this.Token                  = new Token(Json.GetValue("access_token").ToObject<string>(), Json.GetValue("refresh_token").ToObject<string>(), TokenType.Bearer);
                            this.Client.Authenticator   = new OAuth2AuthorizationRequestHeaderAuthenticator(this.Token.AccessKey, "Bearer");
                        }
                        else
                        {
                            this.Token                  = new Token(Json.GetValue("access_token").ToObject<string>(), Json.GetValue("refresh_token").ToObject<string>(), TokenType.Query);
                            this.Client.Authenticator   = new OAuth2UriQueryParameterAuthenticator(this.Token.AccessKey);
                        }

                        this.Token.SetDuration(TimeSpan.FromSeconds(Json.GetValue("expires_in").ToObject<int>()));

                        // Check if authenticated

                        Logging.Info(this.GetType(), "[*] The token expires in " + this.Token.Duration.TotalMinutes + " minute(s).");
                        Logging.Info(this.GetType(), "[*] The token is <" + this.Token.AccessKey + ">.");

                        int VisitorId = this.GetVisitorId();

                        if (VisitorId != -1)
                        {
                            this.IsAuthenticated = true;
                        }
                        else
                        {
                            Logging.Warning(this.GetType(), "[*] VisitorId == -1 at XenforoApi.Authenticate(Username, Password).");
                        }
                    }
                }
                else
                {
                    Logging.Warning(this.GetType(), "[*] Json == null || Json.HasValues != true at XenforoApi.Authenticate(Username, Password).");
                }
            }
            else
            {
                Logging.Warning(this.GetType(), "[*] Response.IsSuccessful != true at XenforoApi.Authenticate(Username, Password).");
                Logging.Warning(this.GetType(), "[*] ErrorCode    = " + Response.StatusCode + ".");
                Logging.Warning(this.GetType(), "[*] ErrorMessage = " + Response.ErrorMessage + ".");
            }
        }

        /// <summary>
        /// Makes the request at the specified url.
        /// </summary>
        private int GetVisitorId()
        {
            var VisitorId   = -1;
            var Request     = new RestRequest();
            var Response    = this.Client.Get(Request);

            if (Response.IsSuccessful)
            {
                var Json = JObject.Parse(Response.Content);

                if (Json != null && Json.HasValues)
                {
                    if (Json.ContainsKey("system_info"))
                    {
                        var SystemArr = Json["system_info"];

                        if (SystemArr.Type != JTokenType.Object)
                        {
                            return -1;
                        }

                        var SystemInfo = SystemArr.ToObject<JObject>();

                        if (SystemInfo != null && SystemInfo.HasValues)
                        {
                            if (SystemInfo.ContainsKey("visitor_id"))
                            {
                                VisitorId = SystemInfo.GetValue("visitor_id").ToObject<int>();
                            }
                            else
                            {
                                Logging.Warning(this.GetType(), "[*] Json.ContainsKey('visitor_id') != true at XenforoApi.GetVisitorId().");
                            }
                        }
                        else
                        {
                            Logging.Warning(this.GetType(), "[*] SystemInfo == null || SystemInfo.HasValues != true at XenforoApi.GetVisitorId()."); ;
                        }
                    }
                    else
                    {
                        Logging.Warning(this.GetType(), "[*] Json.ContainsKey('system_info') != true at XenforoApi.GetVisitorId().");
                    }
                }
                else
                {
                    Logging.Warning(this.GetType(), "[*] Json == null || Json.HasValues != true at XenforoApi.GetVisitorId().");
                }
            }
            else
            {
                Logging.Warning(this.GetType(), "[*] Response.IsSuccessful != true at XenforoApi.GetVisitorId().");

                Logging.Warning(this.GetType(), "[*] ErrorCode    = " + Response.StatusCode + ".");
                Logging.Warning(this.GetType(), "[*] ErrorMessage = " + Response.ErrorMessage + ".");
                Logging.Warning(this.GetType(), "[*] ErrorUrl     = " + Response.ResponseUri + ".");
            }

            return VisitorId;
        }
    }
}