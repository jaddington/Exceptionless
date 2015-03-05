﻿#region Copyright 2014 Exceptionless

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
//     http://www.apache.org/licenses/LICENSE-2.0

#endregion

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Submission.Net;
using Exceptionless.Threading.Tasks;

namespace Exceptionless.Extensions
{
    internal static class WebRequestExtensions
    {
        public const string JSON_CONTENT_TYPE = "application/json";

        public static Task<Stream> GetRequestStreamAsync(this WebRequest request)
        {
           
            
            return Task.Factory.FromAsync<Stream>( request.BeginGetRequestStream, request.EndGetRequestStream, null);
        }

        public static Task<WebResponse> GetResponseAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
        }

        public static void AddAuthorizationHeader(this WebRequest request, ExceptionlessConfiguration configuration)
        {
            var authorizationHeader = new AuthorizationHeader
            {
                Scheme = ExceptionlessHeaders.Bearer,
                ParameterText = configuration.ApiKey
            };

            request.Headers[HttpRequestHeader.Authorization] = authorizationHeader.ToString();
        }

        private static readonly Lazy<PropertyInfo> _userAgentProperty = new Lazy<PropertyInfo>(() => typeof(HttpWebRequest).GetProperty("UserAgent"));

        public static void SetUserAgent(this HttpWebRequest request, ExceptionlessConfiguration configuration)
        {

            // return;

            if (_userAgentProperty.Value != null)
            {
                try
                {
                    _userAgentProperty.Value.SetValue(request, configuration.UserAgent, null);
                    return;
                }
                catch (Exception ex)
                {
                    configuration.Resolver.GetLog().Error(ex, "Error occurred setting the user agent.");
                }
            }

            request.Headers[ExceptionlessHeaders.Client] = configuration.UserAgent;
        }

        public static Task<WebResponse> PostJsonAsync(this HttpWebRequest request, string data)
        {
            request.Accept = request.ContentType = JSON_CONTENT_TYPE;
            request.Method = "POST";

            byte[] buffer = Encoding.UTF8.GetBytes(data);

           
            return request.GetRequestStreamAsync().Then(t =>
            {
                using (var s = t.Result) {

                    s.Write(buffer, 0, buffer.Length);
                }
                return request.GetResponseAsync();
            });

            

        }

        public static Task<WebResponse> GetJsonAsync(this HttpWebRequest request)
        {
            request.Accept = JSON_CONTENT_TYPE;
            request.Method = "GET";

            return request.GetResponseAsync();
        }

    }

    internal static class WebResponseExtensions {
        public static string GetResponseText(this WebResponse response) {
            try {
                using (response) {
                    using (var stream = response.GetResponseStream()) {
                        using (var reader = new StreamReader(stream)) {
                            return reader.ReadToEnd();
                        }
                    }
                }
            } catch (Exception) {
                return null;
            }
        }

        public static bool IsSuccessful(this HttpWebResponse response) {
            return response != null && (int)response.StatusCode >= 200 && (int)response.StatusCode <= 299;
        }
    }
}
