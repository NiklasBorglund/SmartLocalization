//
//  MicrosoftTranslatorManager.cs
//	
//	Information on how to obtain a free clientID and clientSecret from MicrosoftTranslator
//	http://blogs.msdn.com/b/translation/p/gettingstarted1.aspx
// 
// The MIT License (MIT)
//
// Copyright (c) 2013 Niklas Borglund
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MiniJSON;

[System.Serializable]
public class AdmAccessToken
{
	 public AdmAccessToken(){}
     public string access_token { get; set; }
     public string token_type { get; set; }
     public string expires_in { get; set; }
     public string scope { get; set; }
}
public class TranslateCompleteContainer
{
	public WebRequest translationWebRequest{ get; set; }
	//The key to the translated value in a KeyValuePair
	public string dictionaryKey{ get; set; }	
	//The method to call when the translation is complete
	public TranslateCompleteCallback translateCompleteCallback{ get; set; }
}
public class TranslateCompleteArrayContainer
{
	//The key to the translated value in a KeyValuePair
	public List<string> dictionaryKeys{ get; set; }	
	//The method to call when the translation is complete
	public TranslateCompleteArrayCallback translateCompleteCallback{ get; set; }
}
public delegate void TranslateCompleteCallback(string key, string translatedValue);
public delegate void TranslateCompleteArrayCallback(List<string> keys, List<string> translatedValues);

[System.Serializable]
public class MicrosoftTranslatorManager
{
#region Members
	[SerializeField]
	private string clientID;
	[SerializeField]
	private string clientSecret;
	[SerializeField]
	private AdmAccessToken token;
	[SerializeField]
	private string headerValue;
	[SerializeField]
	private bool isInitialized = false;
	[SerializeField]
	private List<string> languagesForTranslate = new List<string>();
#endregion
#region Properties
	//All the languages available for translation
	public List<string> LanguagesAvailableForTranslation
	{
		get
		{
			return languagesForTranslate;	
		}
	}
	public bool IsInitialized
	{
		get
		{
			return isInitialized;	
		}
	}
#endregion

	/// <summary>
	/// Gets the access token.(Async) will set the IsInitialized Variable when done
	/// </summary>
	/// <param name='cliendID'>
	/// Cliend I.
	/// </param>
	/// <param name='clientSecret'>
	/// Client secret.
	/// </param>
	public void GetAccessToken(string cliendID, string clientSecret)
	{
		ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
		
		isInitialized = false;
		//Remove all the possible white spaces
		List<char> cliendIDList = cliendID.ToList();
		cliendIDList.RemoveAll(c => c == ' ');
		this.clientID = new string(cliendIDList.ToArray());
		cliendIDList.Clear();
		
		List<char> clientSecretList = clientSecret.ToList();
		clientSecretList.RemoveAll(c => c == ' ');
		this.clientSecret = new string(clientSecretList.ToArray());
		clientSecretList.Clear();
		
		 // Create the request for the OAuth service that will
		string strTranslatorAccessURI = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
		System.Net.WebRequest req = System.Net.WebRequest.Create(strTranslatorAccessURI);
		req.Method = "POST";
		req.ContentType = "application/x-www-form-urlencoded";
		
		
		  // Important to note -- the call back here isn't that the request was processed by the server
		  // but just that the request is ready for you to do stuff to it (like writing the details)
		  // and then post it to the server.
		req.BeginGetRequestStream(new AsyncCallback(RequestStreamReady), req);
	}
	/// <summary>
	/// Makes the request to the server for the authorization.
	/// </summary>
	private void RequestStreamReady(IAsyncResult ar)
	{
		  string requestDetails = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", 
			clientID, WWW.EscapeURL(clientSecret));	
		
		  HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
		
		 // now that we have the working request, write the request details into it
		 byte[] bytes = System.Text.Encoding.UTF8.GetBytes(requestDetails);
		 System.IO.Stream postStream = request.EndGetRequestStream(ar);
		 postStream.Write(bytes, 0, bytes.Length);
		 postStream.Close();
		
		
		  // now that the request is good to go, let's post it to the server
		  // and get the response. When done, the async callback will call the
		 // GetResponseCallback function
		 request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
	}
	/// <summary>
	/// Gets the response callback for authentication
	/// </summary>
	/// <param name='ar'>
	/// the result of the callback
	/// </param>
	private void GetResponseCallback(IAsyncResult ar)
	{
		
				
	  // Process the response callback to get the token
	  // we'll then use that token to call the translator service
	  // Pull the request out of the IAsynch result
	  	HttpWebRequest request = (HttpWebRequest)ar.AsyncState;
	  // The request now has the response details in it (because we've called back having gotten the response
		 HttpWebResponse response = null;
		try
		{
	   		response = (HttpWebResponse)request.EndGetResponse(ar);
		}
		catch (WebException e)
        {
			response = null;
            using (response = (HttpWebResponse)e.Response)
            {
				//Error
                HttpWebResponse httpResponse = (HttpWebResponse) response;
                Debug.Log("Error code:" +  httpResponse.StatusCode.ToString());
                using (Stream data = response.GetResponseStream())
                {
                    string text = new StreamReader(data).ReadToEnd();
                    Debug.Log(text);
                }
            }
        }
	  // Using JSON we'll pull the response details out, and load it into an AdmAccess token class 
	  try
	  {		
			//Save the token and the header value for translation
			StreamReader streamReader = new StreamReader(response.GetResponseStream());
			string jsonObject = streamReader.ReadToEnd();
			streamReader.Close();
			
			token = new AdmAccessToken();
			Dictionary<string,object> accessTokenValues = Json.Deserialize(jsonObject) as Dictionary<string,object>;
			foreach(KeyValuePair<string,object> tokenValue in accessTokenValues)
			{
				if(tokenValue.Key == "access_token")
				{
					token.access_token = tokenValue.Value.ToString();
				}
				else if(tokenValue.Key == "token_type")
				{
					token.token_type = tokenValue.Value.ToString();
				}
				else if(tokenValue.Key == "expires_in")
				{
					token.expires_in = tokenValue.Value.ToString();
				}
				else if(tokenValue.Key == "scope")
				{
					token.scope = tokenValue.Value.ToString();
				}
			}
			headerValue = "Bearer " + token.access_token;
			
			//Set the translator manager to initialized
			Debug.Log("Microsoft Translator is authenticated");
			isInitialized = true;
			GetAllTranslationLanguages();
	  }
	  catch (Exception ex)
	  {
		 Debug.LogError("MicrosoftTranslatorManager.cs:" + ex.Message);
	  }
	  finally
      {
            if (response != null)
            {
                response.Close();
                response = null;
            }
      }
	}
	/// <summary>
	/// Recieves the translation and invokes the callback method sending the translated value
	/// </summary>
	/// <param name='ar'>
	/// Ar.
	/// </param>
	private void TranslationReady(IAsyncResult ar)
	{
		//Get the container
	  	TranslateCompleteContainer translationContainer = (TranslateCompleteContainer)ar.AsyncState;
	  	// Get the request details
	 	HttpWebRequest request = (HttpWebRequest)translationContainer.translationWebRequest;
	  	// Get the response details
	  	HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ar);
	  	// Read the contents of the response into a string
	  	System.IO.Stream streamResponse = response.GetResponseStream();

		//Read the translated value from the xml document recieved from microsoft translate
        XmlReader reader = XmlReader.Create(streamResponse);
		reader.Read();
		reader.Read();
		string translatedValue = reader.Value;
		reader.Close();
		
		//Invoke the callback method
		if(translationContainer.translateCompleteCallback != null)
		{
			translationContainer.translateCompleteCallback(translationContainer.dictionaryKey, translatedValue);
		}
		translationContainer.translateCompleteCallback = null;
		translationContainer = null;
		
		//Close the response and clear the reference
		if (response != null)
        {
            response.Close();
            response = null;
        }
	}
	/// <summary>
	/// Gets a list of all the languages available in the Microsoft Translator API
	/// </summary>
	private void GetAllTranslationLanguages()
	{
		    string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForTranslate";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", headerValue);
            WebResponse response = null;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
					XmlReader reader = XmlReader.Create(stream);
					languagesForTranslate.Clear();
					while (reader.Read())
			        {
			            if(reader.NodeType == XmlNodeType.Element)
			            {
			                if (reader.Name == "ArrayOfstring")
			                {
								if (reader.ReadToDescendant("string"))
						        {
						            do
						            {
						                languagesForTranslate.Add(reader.ReadString());
						            }
						            while (reader.ReadToNextSibling("string"));
						        }
			                }
			            }
			        }
					reader.Close();	
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
	}
	
	/// <summary>
	/// Translates the text.
	/// </summary>
	/// <param name='textToTranslate'>
	/// Text to translate.
	/// </param>
	/// <param name='languageFrom'>
	/// Language from.
	/// </param>
	/// <param name='languageTo'>
	/// Language to.
	/// </param>
	/// <param name='key'>
	/// Key. This value will be returned in the callback
	/// </param>
	/// <param name='callbackMethod'>
	/// Callback method.
	/// </param>
	public void TranslateText(string textToTranslate, string languageFrom, string languageTo, string key, TranslateCompleteCallback callbackMethod)
	{
		if(IsInitialized)
		{
			string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + WWW.EscapeURL(textToTranslate) + "&from=" + languageFrom + "&to=" + languageTo;
	   		WebRequest translationWebRequest = HttpWebRequest.Create(uri);
	    	translationWebRequest.Headers["Authorization"] = headerValue;
		
			TranslateCompleteContainer translationContainer = new TranslateCompleteContainer();
			translationContainer.dictionaryKey = key;
			translationContainer.translationWebRequest = translationWebRequest;
			translationContainer.translateCompleteCallback += callbackMethod;
			
	    	// And now we call the service. When the translation is complete, we'll get the callback
			translationWebRequest.BeginGetResponse(new AsyncCallback(TranslationReady), translationContainer);
		}
		else
		{
			Debug.LogError("MicrosoftTranslatorManager is not authenticated, use the GetAccessToken to authenticate");
		}
	}
	
	
	 public void TranslateArray(List<string> textsToTranslate, string languageFrom, string languageTo, List<string> keys, TranslateCompleteArrayCallback callbackMethod)
     {
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/TranslateArray";
			//Generate the request body
            string body = "<TranslateArrayRequest>" +
                             "<AppId />" +
                             "<From>";
			body += languageFrom;
			body += "</From>" +
                    "<Options>" +
                    " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                    "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">text/plain</ContentType>" +
                    "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                    "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                    "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                    "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                    "</Options>" +
                    "<Texts>";
			foreach(string text in textsToTranslate)
			{
				body += "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">" + text + "</string>";
			}
            body += "</Texts>" +"<To>";
			body += languageTo;
			body += "</To>" + "</TranslateArrayRequest>";
		
            // create the request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers.Add("Authorization", headerValue);
            request.ContentType = "text/xml";
            request.Method = "POST";

            using (System.IO.Stream stream = request.GetRequestStream())
            {
                byte[] arrBytes = System.Text.Encoding.UTF8.GetBytes(body);
                stream.Write(arrBytes, 0, arrBytes.Length);
            }

            // Get the response
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {		
						List<string> translatedTexts = new List<string>();
					    XmlReader reader = XmlReader.Create(stream);
						while (reader.Read())
			        	{
			            	if(reader.NodeType == XmlNodeType.Element)
			           	 	{
			              	  if (reader.Name == "TranslateArrayResponse")
			               	  {
									if (reader.ReadToDescendant("TranslatedText"))
						        	{
						            	do
						            	{
						             	   translatedTexts.Add(reader.ReadString());
						           	 	}
						            	while (reader.ReadToNextSibling("TranslatedText"));
						        	}
			               	  }
			            }
			        	}
						reader.Close();
					
						if(callbackMethod != null)
						{
							callbackMethod(keys, translatedTexts);
						}
						callbackMethod = null;
						keys.Clear();
						translatedTexts.Clear();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
	}
	public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
	{
    	return true;
	}
}
