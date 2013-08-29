/*
 * This is sample code that shows how a Batch client application can obtain access to the Revolution Web API, and
 * is provided on an AS-IS basis.  This isn't production quality code; it is mainly intended to show the techniques
 * involved in talking to the StatPro Revolution OAuth2 Server from a Batch app.
 * 
 * To access data from the Revolution Web API on behalf of a user, the user must have created a "batch authorization"
 * for the Batch application in question on the StatPro Revolution API Authorization Management website, whose address
 * is https://revapiauth.statpro.com  This process results in the generation of an application-specific password (ASP).
 * The user's username (= email address) and ASP must both be made known to the Batch application, which must store
 * this data privately and securely.  At runtime, the application obtains an access token from the StatPro Revolution
 * OAuth2 Server for a user by submitting the user's username and accompanying ASP to the OAuth2 Server's token
 * endpoint via the OAuth 2.0 Resource Owner Password flow.
 * 
 * Note that Batch applications are not issued refresh tokens; getting a new access token for a particular user
 * (after the previous one expires) is done in the same way as getting an access token in the first place.
 * 
 * This sample does NOT show a lot of useful and necessary techniques:-
 *     - getting portfolios, analysis and results data from the Web API
 *     - detecting if the Web API has returned one of its specific errors
 *     - detecting request blockage by the Web API due to a Fair Usage Policy violation
 *     - detecting if the Web API has rejected the access token because it has expired
 * All of these things and more are shown by the "StatPro Revolution Web API Explorer" repository on GitHub.
 */
using System;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace RevolutionWebApiBatchApp
{
    class Program
    {
        // StatPro Revolution's OAuth2 Server's token endpoint.
        const String AuthServerTokenUri = "https://revapiaccess.statpro.com/OAuth2/Token";

        // This application's unique client identifier, obtained when registering the Batch application with
        // StatPro for OAuth2 / Web API access.
        const String ClientId = "<your client id goes here>";

        // The Revolution Web API's scope identifier.
        const String RevolutionWebApiScopeId = "RevolutionWebApi";

        // Web API entry point.
        const String WebApiEntryPointUri = "https://revapi.statpro.com/v1";

        // Program entry-point.
        static void Main(string[] args)
        {
            MainAsync().Wait();

#if DEBUG
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
#endif
        }

        // Async version of Main.
        static async Task MainAsync()
        {
            // Get access token from username and application-specific password.
            String accessToken = null;
            try
            {
                var userCreds = GetUserCredentials();
                accessToken = await GetAccessTokenAsync(userCreds.Item1, userCreds.Item2);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to get access token.  " + ex.Message);
                return;
            }

            // Get the Service resource from Revolution Web API using the access token.
            String service = null;
            try
            {
                service = await GetServiceResourceFromWebApiAsync(accessToken);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to get resource from the Revolution Web API.  " + ex.Message);
                return;
            }

            // Output the number of portfolios that the user has.
            var doc = XDocument.Parse(service);
            var ns = (XNamespace) "http://statpro.com/2012/Revolution";
            var total = doc.Element(ns + "service").Element(ns + "portfolios").Element(ns + "total").Value;
            Console.Out.WriteLine("The user has " + total.ToString(CultureInfo.InvariantCulture) + " portfolios.");

            return;
        }

        // Gets an access token from the specified username (= email address) and password (= application-specific
        // password), using the OAuth 2.0 Resource Owner Password flow.
        // The returned task will return the access token.  The task will thrown an exception if an error occurs.
        private static async Task<String> GetAccessTokenAsync(String username, String password)
        {
            // Issue a POST request to swap the username and password for an access token, in accordance with
            // http://tools.ietf.org/html/rfc6749#section-4.3.2
            // Note that 'scope' is required by the StatPro Revolution OAuth2 Server.
            String content;
            HttpClient httpClient = null;
            try
            {
                httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(ClientId + ":" + GetClientSecret()))
                );

                var formUrlEncodedContent = new FormUrlEncodedContent(
                    new List<KeyValuePair<String, String>>()
                    { 
                        new KeyValuePair<String, String>("grant_type", "password"),
                        new KeyValuePair<String, String>("username", username),
                        new KeyValuePair<String, String>("password", password),
                        new KeyValuePair<String, String>("scope", RevolutionWebApiScopeId),
                    });

                var response = await httpClient.PostAsync(AuthServerTokenUri, formUrlEncodedContent);

                content = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                // Parse the content as JSON.  Here we show the full range of information that can be extracted
                // from the response.  Unlike other application types, Batch applications do not receive a refresh
                // token since they can re-submit the username and password programmatically to get another access
                // token.
                var jsonObj = JObject.Parse(content);
                var accessToken = jsonObj["access_token"].ToString();
                var expiresIn = jsonObj["expires_in"].ToString();   // time in seconds
                var scope = jsonObj["scope"].ToString();            // should be "RevolutionWebApi"
                var tokenType = jsonObj["token_type"].ToString();   // should be "Bearer"
                var userId = jsonObj["user_id"].ToString();         // the user's unique identifier
                var userName = jsonObj["user_name"].ToString();     // the user's non-unique name

                return accessToken;
            }

            catch
            {
                // Note: 'content' *may* contain JSON that contains an error response from the OAuth2 Server,
                // containing both error code and error description; e.g.
                //   {
                //     "error": "invalid_grant",
                //     "error_description": "Username and/or password is invalid, or they apply to another client."
                //   }
                //
                // Note that a user may revoke a previously generated ASP on the StatPro Revolution API Authorization
                // Management website (https://revapiauth.statpro.com/), and this will cause a failure to get an
                // access token at runtime until such time as the user creates a new "batch authorization" and makes
                // its ASP known to the Batch application.
                //
                // An error code that is of particular importance to Batch applications is the non-standard code
                // "termsofuse_not_accepted" (i.e. this is not a standard OAuth 2.0 error code).  This error code
                // indicates that the user in question hasn't accepted the latest version(s) of the Terms of Use
                // that cover the Revolution Web API.  Whereas the user of a Server-Side Web application or a Native
                // application will be prompted to accept the latest Terms of Use (if unaccepted) at runtime, this
                // doesn't apply to Batch applications since there is no UI/UX at runtime.  Instead the
                // "termsofuse_not_accepted" error code is returned, and the Batch application should log that the
                // user in question should visit https://revapiauth.statpro.com/ to view their accessible resource
                // services and their covering Terms of Use.
                // 
                // For more details of the errors that can be returned, please see the StatPro Revolution OAuth2 Server
                // documentation:- http://developer.statpro.com/Revolution/WebApi/Authorization

                throw;
            }

            finally
            {
                if (httpClient != null)
                    httpClient.Dispose();
            }
        }

        // Returns a task that will return the XML representation of the Revolution Web API's Service resource.
        // The task will thrown an exception if an error occurs.
        private static async Task<String> GetServiceResourceFromWebApiAsync(String accessToken)
        {
            HttpClient httpClient = null;
            try
            {
                httpClient = new HttpClient();

                // Set the Authorization header to identify the user.  The access token is not base64-encoded here
                // because it is expected to already be base64-encodedi.
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Set the Accept header to indicate that we want an XML based resource representation.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(
                        "application/xml"
                    ));

                // Send a GET request for the resource and await the response.
                var response = await httpClient.GetAsync(WebApiEntryPointUri);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }

            finally
            {
                if (httpClient != null)
                    httpClient.Dispose();
            }
        }

        // Get this client application's 'client secret'.  How secret the "secret" is depends on the nature
        // of the application, how it is written, how it stores its secret, what environment it runs on, how
        // widely it is distributed, etc. etc.  Broadly speaking, publicly-available client applications cannot
        // keep secrets, in which case the client secret isn't a secret.  Nevertheless, such applications shouldn't
        // publicly expose the client secret.
        // For further details: http://tools.ietf.org/html/rfc6819#section-4.1.1
        // The client secret is originally obtained upon successful registration of your app with StatPro.
        private static String GetClientSecret()
        {
            return "<it's up to you how the client secret is stored and retrieved>";
        }

        // This sample application accesses data on behalf of just one user, and this method returns that user's
        // username (= email address) and application-specific password (ASP) in a two-item tuple, with the username
        // in the first item.  See the comment at the top of this file about creating a batch authorization and
        // obtaining the ASP for a user from the API Authorization Management website.
        private static Tuple<String, String> GetUserCredentials()
        {
            // A real application must get this information from secure/private storage.  A user's ASP must not
            // be publicly accessible; ideally the user's email address won't be either.
            return Tuple.Create("<username>", "<asp>");
        }
    }
}
