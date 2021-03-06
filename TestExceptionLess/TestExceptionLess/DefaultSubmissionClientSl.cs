﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Browser;
using Exceptionless;
using Exceptionless.Configuration;
using Exceptionless.Extensions;
using Exceptionless.Json.Linq;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission;
using Exceptionless.Submission.Net;

namespace TestExceptionLess
{
    public class DefaultSubmissionClientSl : ISubmissionClient {
        public SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            var data = serializer.Serialize(events);

            HttpWebResponse response;
            try {
                var request = CreateHttpWebRequest(config, "events");

                response = request.PostJsonAsync(data).Result as HttpWebResponse;
            } catch (AggregateException aex) {
                var ex = aex.GetInnermostException() as WebException;
                if (ex != null)
                    response = (HttpWebResponse)ex.Response;
                else
                    return new SubmissionResponse(500, message: aex.GetMessage());
            } catch (Exception ex) {
                return new SubmissionResponse(500, message: ex.Message);
            }

            int settingsVersion;
            if (Int32.TryParse(response.Headers[ExceptionlessHeaders.ConfigurationVersion], out settingsVersion))
                SettingsManager.CheckVersion(settingsVersion, config);

            return new SubmissionResponse((int)response.StatusCode, GetResponseMessage(response));
        }

        public SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            var data = serializer.Serialize(description);

            HttpWebResponse response;
            try {
                var request = CreateHttpWebRequest(config, String.Format("events/by-ref/{0}/user-description", referenceId));
                response = request.PostJsonAsync(data).Result as HttpWebResponse;
            } catch (AggregateException aex) {
                var ex = aex.GetInnermostException() as WebException;
                if (ex != null)
                    response = (HttpWebResponse)ex.Response;
                else
                    return new SubmissionResponse(500, message: aex.GetMessage());
            } catch (Exception ex) {
                return new SubmissionResponse(500, message: ex.Message);
            }

            return new SubmissionResponse((int)response.StatusCode, GetResponseMessage(response));
        }

        public SettingsResponse GetSettings(ExceptionlessConfiguration config, IJsonSerializer serializer) {
            HttpWebResponse response;
            try {
                var request = CreateHttpWebRequest(config, "projects/config");
                response = request.GetJsonAsync().Result as HttpWebResponse;
            } catch (Exception ex) {
                var message = String.Concat("Unable to retrieve configuration settings. Exception: ", ex.GetMessage());
                return new SettingsResponse(false, message: message);
            }

            if (response == null || response.StatusCode != HttpStatusCode.OK)
                return new SettingsResponse(false, message: String.Format("Unable to retrieve configuration settings: {0}", GetResponseMessage(response)));

            var json = response.GetResponseText();
            if (String.IsNullOrWhiteSpace(json))
                return new SettingsResponse(false, message: "Invalid configuration settings.");

            var settings = serializer.Deserialize<ClientConfiguration>(json);
            return new SettingsResponse(true, settings.Settings, settings.Version);
        }

        private static string GetResponseMessage(HttpWebResponse response) {
            if (response.IsSuccessful())
                return null;

            int statusCode = (int)response.StatusCode;
            string responseText = response.GetResponseText();
            string message = statusCode == 404 ? "404 Page not found." : responseText.Length < 500 ? responseText : "";

            if (responseText.Trim().StartsWith("{")) {
                try {
                    var responseJson = JObject.Parse(responseText);
                    message = responseJson["message"].Value<string>();
                } catch {}
            }

            return message;
        }



        private HttpWebRequest CreateHttpWebRequest(ExceptionlessConfiguration config, string endPoint) {

            //    var r2 =  (HttpWebRequest)WebRequestCreator.ClientHttp.Create(new Uri(
            //"http://api.search.live.net/qson.aspx?query=Silverlight"));

            var request = (HttpWebRequest)WebRequestCreator.ClientHttp.Create(new Uri(String.Concat(config.GetServiceEndPoint(), endPoint)));

            var request2 = (HttpWebRequest)WebRequestCreator.BrowserHttp.Create(new Uri(String.Concat(config.GetServiceEndPoint(), endPoint)));

            var authorizationHeader = new AuthorizationHeader {
                Scheme = ExceptionlessHeaders.Bearer,
                ParameterText = config.ApiKey
            };

            request.Headers["UserAgent"] = config.UserAgent;
            //request.UserAgent = config.UserAgent;
            request.Headers[ExceptionlessHeaders.Client] = config.UserAgent;
            request.Headers[HttpRequestHeader.Authorization] = authorizationHeader.ToString();

            return request;
        }

    }
}
