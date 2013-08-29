
# Introduction #

This is a lightweight sample that shows how a Batch application - in this case a Windows Console application - can get user data from the StatPro Revolution Web API.  This involves talking to the StatPro Revolution OAuth2 Server to get an access token from a user's username (= email address) in combination with a special type of password known as an application-specific password (ASP).

The main point of a Batch application is that it doesn't need to prompt the user for access to his/her data at runtime.  Instead users must visit the [StatPro Revolution API Authorization Management website](https://revapiauth.statpro.com) and create a "batch authorization" that will allow a Batch application to access their data later on, i.e. at runtime.  This process results in the generation of an application-specific password (ASP).  The user's username (= email address) and ASP must both be made known to the Batch application, which must store this data privately and securely.  At runtime, the application obtains an access token from the StatPro Revolution OAuth2 Server for a user by submitting the user's username and accompanying ASP to the OAuth2 Server's token endpoint via the OAuth 2.0 Resource Owner Password flow.

Note that a user may revoke a previously generated ASP on the [StatPro Revolution API Authorization Management website](https://revapiauth.statpro.com/), and this will cause a failure by the application to get an access token at runtime until such time as the user creates a new "batch authorization" and makes its ASP known to the Batch application.

The sample is written in C# 5 and .NET 4.5.

This is not production quality code.  It is intended to show certain useful techniques that are particular to a Batch application.  Real applications should be coded more carefully, paying particular attention to OAuth 2.0 security issues.

*You should not expect the sample application listed here to run successfully.  It requires a genuine client identifier and client secret to be made available, and this information is only made available to you when you register your own Batch client application.*


# Revolution Web API #

The StatPro Revolution Web API allows client applications to access user data from the [StatPro Revolution system](http://www.statpro.com/cloud-based-portfolio-analysis/revolution/) programmatically.

User authentication and authorization is handled by StatPro OAuth2 Server, which in the case of Batch applications uses OAuth 2.0's 'Resource Owner Password' flow.

To run your own Batch application, you must first register it with StatPro.

For more information:-
* [Revolution Web API](http://developer.statpro.com/Revolution/WebApi/Intro)
* [Revolution OAuth2 Server](http://developer.statpro.com/Revolution/WebApi/Authorization/Overview)
* [Registering your own application](http://developer.statpro.com/Revolution/WebApi/Authorization/Registration)
* [Revolution OAuth2 application workflow](http://developer.statpro.com/Revolution/WebApi/Authorization/Workflow)
* [OAuth 2.0](http://tools.ietf.org/html/rfc6749)
* [OAuth 2.0 client security considerations](http://tools.ietf.org/html/rfc6819#section-4.1)
* [Revolution Web API and OAuth2 Support](mailto:webapisupport@statpro.com)


# What the sample demonstrates #

The sample demonstrates:-
* swapping a username (= email address) and application-specific password (ASP) for an access token
* requesting data from the Web API, using the access token.


# What the sample does not demonstrate #

The following techniques are not demonstrated by this simple sample application.  Nevertheless, they should be implemented by production-quality client applications:-
* getting [portfolios](http://developer.statpro.com/Revolution/WebApi/Resource/Portfolios), [analysis](http://developer.statpro.com/Revolution/WebApi/Resource/PortfolioAnalysis) and results data from the Web API
* detecting if the Web API has returned one of its [specific errors](http://developer.statpro.com/Revolution/WebApi/Intro#statusCodes)
* detecting request blockage by the Web API due to a [Fair Usage Policy violation](http://developer.statpro.com/Revolution/WebApi/FairUsagePolicy)
* detecting if the Web API has [rejected the access token because it has expired](http://developer.statpro.com/Revolution/WebApi/Authorization/Workflow#step4)

Please see other samples on GitHub that cover these techniques.

Another technique that is not demonstrated, and which is particular to Batch applications, is detecting if the request for an access token failed because the user hasn't accepted the latest version(s) of the Terms of Use that cover the Revolution Web API.  For more details, see the relevant comment in the sample's Program.cs file. 


# License #


THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

 