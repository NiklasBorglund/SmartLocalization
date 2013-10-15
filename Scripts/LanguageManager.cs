//
//  LanguageManager.cs
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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Globalization;

/// <summary>
/// Change language event handler.
/// </summary>
public delegate void ChangeLanguageEventHandler(LanguageManager thisLanguage);

public class LanguageManager : MonoBehaviour
{
    #region Singleton
    private static LanguageManager instance = null;
    public static LanguageManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject();
                instance = go.AddComponent<LanguageManager>();
                go.name = "LanguageManager";
            }

            return instance;
        }
    }
	public static bool HasInstance
	{
		get
		{
			return (instance != null);
		}
	}
    #endregion
	
	/// <summary>
	/// Occurs when a new language is loaded and initialized
	/// create a delegate method(ChangeLanguage(LanguageManager thisLanguage, CultureInfo newLanguage)) and subscribe
	/// </summary>
	public event ChangeLanguageEventHandler OnChangeLanguage;
	
	/// <summary>
	/// The language that the system will try and load if LoadResources is called.
	/// </summary>
	public string language = "en";
	/// <summary>
	/// The default language.
	/// </summary>
    public string defaultLanguage = "en";
	
	/// <summary>
	/// The path to use in the Resources.Load Method.
	/// </summary>
    private string resourceFile = "Localization/Generated Assets/Language";

	/// <summary>  The header to add to the xmlFile after the unwanted resx-stuff is removed(To read the file with XMLReader)  </summary>
    private string xmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>";
	/// <summary> Dictionary with the raw text elements   </summary>
	private SortedDictionary<string, string> textDataBase = new SortedDictionary<string,string>();
	/// <summary> The parsed localizedObject list  </summary>
	private SortedDictionary<string, LocalizedObject> localizedObjectDataBase = new SortedDictionary<string, LocalizedObject>();
	
	/// <summary>
	/// a bool which indicates if a language is loaded
	/// </summary>
    private bool initialized = false;
	
	//A list of all the available languages
	private List<string> availableLanguages = new List<string>();
	/// <summary>
	/// Gets a list of the available languages.
	/// </summary>
	/// <value>
	/// The available languages.
	/// </value>
	public List<string> AvailableLanguages
	{
		get
		{
			return availableLanguages;	
		}
	}
	
#if !UNITY_WP8
	//Same as availableLanguages, but contains more info in the form of System.Globalization.CultureInfo
	private List<CultureInfo> availableLanguagesCultureInfo = new List<CultureInfo>();
	/// <summary>
	/// Gets the available languages culture info.(Same as availableLanguages, but contains more info in the form of System.Globalization.CultureInfo)
	/// </summary>
	/// <value>
	/// The available languages culture info.
	/// </value>
	public List<CultureInfo> AvailableLanguagesCultureInfo
	{
		get
		{
			return availableLanguagesCultureInfo;	
		}
	}
#endif
	
	/// <summary>
	/// a bool which indicates if a language is loaded
	/// </summary>
	/// <value>
	/// <c>true</c> if a language is loaded; otherwise, <c>false</c>.
	/// </value>
    public bool IsInitialized
    {
        get { return initialized; }
    }
	
	void Awake ()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad (this.gameObject);
		}
		
		if(PlayerPrefs.HasKey("cws_defaultLanguage"))
		{
			defaultLanguage = PlayerPrefs.GetString("cws_defaultLanguage");	
		}
		
		GetAvailableLanguages();

		Debug.Log ("LanguageManager.cs: Waking up");
		
		//Load the default language(if it exists)
		foreach(string availableLanguage in availableLanguages)
		{
			if(availableLanguage == defaultLanguage)
			{
				ChangeLanguage(availableLanguage);
				break;
			}
		}
		
		//otherwise - load the first language in the list
		if(!initialized && availableLanguages.Count > 0)
		{
			ChangeLanguage(availableLanguages[0]);
		}
		else if(!initialized)
		{
			Debug.LogError("LanguageManager.cs: No language is available! Use Window->Smart Localization tool to create a language");	
		}
	}
	void OnDestroy()
	{
		//Clear the event handler
		OnChangeLanguage = null;
	}
	
	/// <summary>
	/// Loads the language file, language is specified with the language variable
	/// </summary>
    private void LoadResources()
    {
        //Reset values
        initialized = false;
        textDataBase.Clear();
		localizedObjectDataBase.Clear();
		
		//Load the root file if language is null
		TextAsset languageDocument;
		if(language == null)
		{
			languageDocument = Resources.Load(resourceFile) as TextAsset;
		}
		else
		{
			languageDocument = Resources.Load(resourceFile + "." + language) as TextAsset;
		}

        if (!languageDocument && defaultLanguage != language)
        {
            //If the language does not exist, revert back to the default language and reload
            Debug.LogError("ERROR: Language file:" + language + " could not be found! - reverting to default language:" + defaultLanguage);
            ChangeLanguage(defaultLanguage);
            return;
        }
		else if(!languageDocument)
		{
			Debug.LogError("ERROR: Language file:" + language + " could not be found!");
			return;
		}

        //Remove beginning of the file that we don't need 
        //The part of the .resx file that  
        int length = "</xsd:schema>".Length;
        string resxDocument = languageDocument.text;
        int index = resxDocument.IndexOf("</xsd:schema>");
        index += length;
        string xmlDocument = resxDocument.Substring(index);

        //add the header to the document
        xmlDocument = xmlHeader + xmlDocument;

        //Create the xml file with the new reduced resx document
        XmlReader reader = XmlReader.Create(new StringReader(xmlDocument));

        //read through the document and save the data
        ReadElements(reader);
        
        //done
        reader.Close();
        initialized = true;
    }
	/// <summary>
	/// Reads the elements from the loaded xmldocument in LoadResources
	/// </summary>
	/// <param name='reader'>
	/// Reader.
	/// </param>
    private void ReadElements(XmlReader reader)
    {
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    //If this is a chunk of data, then parse it
                    if (reader.Name == "data")
                    {
                        ReadData(reader);
                    }
                    break;
            }
        }
    }
	/// <summary>
	/// Reads a specific data tag from the xml document.
	/// </summary>
	/// <param name='reader'>
	/// Reader.
	/// </param>
    private void ReadData(XmlReader reader)
    {
        //If these values are not being set,
        //something is wrong.
        string key = "ERROR";

        string value = "ERROR";

        if (reader.HasAttributes)
        {
            while (reader.MoveToNextAttribute())
            {
                if (reader.Name == "name")
                {
                    key = reader.Value;
                }
            }
        }

        //Move back to the element
        reader.MoveToElement();

        //Read the child nodes
        if (reader.ReadToDescendant("value"))
        {
            do
            {
                value = reader.ReadString();
            }
            while (reader.ReadToNextSibling("value"));
        }

        //Add the raw values to the dictionary
        textDataBase.Add(key, value);
		
		//Add the localized parsed values to the localizedObjectDataBase
		LocalizedObject newLocalizedObject = new LocalizedObject();
		newLocalizedObject.ObjectType = LocalizedObject.GetLocalizedObjectType(key);
		newLocalizedObject.TextValue = value;
		localizedObjectDataBase.Add(LocalizedObject.GetCleanKey(key,newLocalizedObject.ObjectType), newLocalizedObject); 
    }
	/// <summary>
	/// Gets all the available languages.
	/// </summary>
	private void GetAvailableLanguages()
	{
		//Clear the available languages list
		availableLanguages.Clear();
#if !UNITY_WP8
		availableLanguagesCultureInfo.Clear();
#endif
		
		Object[] languageFiles = Resources.LoadAll("Localization/Generated Assets", typeof(TextAsset));
		string languageStart = "Language.";
		foreach(Object languageFile in languageFiles)
		{
			if(languageFile.name != "Language") //Skip the root file
			{
				if(languageFile.name.StartsWith(languageStart))
				{
					string thisLanguageName = languageFile.name.Substring(languageStart.Length);
					availableLanguages.Add(thisLanguageName);
#if !UNITY_WP8
					availableLanguagesCultureInfo.Add(CultureInfo.GetCultureInfo(thisLanguageName));
#endif
				}
			}
		}
		
	}
	
	/// <summary>
	/// Returns a text value in the current language for the key. Returns null if nothing is found.
	/// </summary>
	/// <returns>
	/// The value in the specified language, returns null if nothing is found
	/// </returns>
	/// <param name='key'>
	/// The Language Key.
	/// </param>
    public string GetTextValue(string key)
    {
		LocalizedObject thisObject = GetLocalizedObject(key);
      
		if(thisObject != null)
		{
			return thisObject.TextValue;	
		}

		Debug.LogError("LanguageManager.cs: Invalid Key:" + key + "for language: " + language);
        return null;
    }
	/// <summary>
	/// Gets the audio clip for the current language, returns null if nothing is found
	/// </summary>
	/// <returns>
	/// The audio clip. Null if nothing is found
	/// </returns>
	/// <param name='key'>
	/// Key.
	/// </param>
	public AudioClip GetAudioClip(string key)
	{
		LocalizedObject thisObject = GetLocalizedObject(key);
      
		if(thisObject != null)
		{
			return Resources.Load("Localization/" + language + "/Audio Files/" + key) as AudioClip;
		}

        return null;
	}
	/// <summary>
	/// Gets the prefab game object for the current language, returns null if nothing is found
	/// </summary>
	/// <returns>
	/// The loaded prefab. Null if nothing is found
	/// </returns>
	/// <param name='key'>
	/// Key.
	/// </param>
	public GameObject GetPrefab(string key)
	{
		LocalizedObject thisObject = GetLocalizedObject(key);
      
		if(thisObject != null)
		{
			return Resources.Load("Localization/" + language + "/Prefabs/" + key) as GameObject;
		}

        return null;
	}	
	/// <summary>
	/// Gets the texture for the current language, returns null if nothing is found
	/// </summary>
	/// <returns>
	/// The loaded prefab. Null if nothing is found
	/// </returns>
	/// <param name='key'>
	/// Key.
	/// </param>
	public Texture GetTexture(string key)
	{
		LocalizedObject thisObject = GetLocalizedObject(key);
      
		if(thisObject != null)
		{
			return Resources.Load("Localization/" + language + "/Textures/" + key) as Texture;
		}

        return null;
	}
	
	/// <summary>
	/// Gets the localized object from the localizedObjectDataBase
	/// </summary>
	/// <returns>
	/// The localized object. Returns null if nothing is found
	/// </returns>
	/// <param name='key'>
	/// Key.
	/// </param>
	private LocalizedObject GetLocalizedObject(string key)
	{
		LocalizedObject thisObject;
		localizedObjectDataBase.TryGetValue(key, out thisObject);
      

        return thisObject;
	}
	
	/// <summary>
	/// Changes the language and tries to load it.
	/// </summary>
	/// <param name='language'>
	/// Language.
	/// </param>
    public void ChangeLanguage(string language)
    {
        this.language = language;
        LoadResources();
		
		if(IsInitialized && OnChangeLanguage != null)
		{
			OnChangeLanguage(this);	
		}
    }
	/// <summary>
	/// Returns the entire RAW language database from the loaded language in a Dictionary
	/// it contains type keys and everything
	/// </summary>
	/// <returns>
	/// The text data base.
	/// </returns>
	public Dictionary<string, string> GetTextDataBase()
	{
		//Convert the sorted dictionary to a new, regular one
		return new Dictionary<string,string>(textDataBase);	
	}
	/// <summary>
	/// Gets the localized, clean and parsed object data base. 
	/// </summary>
	/// <returns>
	/// The localized object data base.
	/// </returns>
	public Dictionary<string, LocalizedObject> GetLocalizedObjectDataBase()
	{
		//Convert the sorted dictionary to a new, regular one
		return new Dictionary<string,LocalizedObject>(localizedObjectDataBase);	
	}
	/// <summary>
	/// Clear this instance and Destroys it
	/// </summary>
	public void Clear ()
	{
		instance = null;
		DestroyImmediate(this.gameObject);
	}
	/// <summary>
	/// Sets the default language. This language will be loaded in Awake() if it exists
	/// By default this is set to = "en"
	/// </summary>
	/// <param name='languageName'>
	/// Language name.
	/// </param>
	public void SetDefaultLanguage(string languageName)
	{
		PlayerPrefs.SetString("cws_defaultLanguage", languageName);
	}
	/// <summary>
	/// Sets the default language. This language will be loaded in Awake() if it exists
	/// By default this is set to = "en"
	/// </summary>
	/// <param name='languageName'>
	/// Language name.
	/// </param>
	public void SetDefaultLanguage(CultureInfo languageInfo)
	{
		SetDefaultLanguage(languageInfo.Name);
	}
	/// <summary>
	/// Checks if the language is supported by this application
	/// languageName = strings like "en", "es", "sv"
	/// </summary>
	public bool IsLanguageSupported(string languageName)
	{
		return availableLanguages.Contains(languageName);
	}
	/// <summary>
	/// Checks if the language is supported by this application
	/// </summary>
	public bool IsLanguageSupported(CultureInfo cultureInfo)
	{
		return IsLanguageSupported(cultureInfo.Name);
	}
	/// <summary>
	/// Gets the system language for this application using Application.systemLanguage
	/// If its SystemLanguage.Unknown, a string with the value "Unknown" will be returned
	/// </summary>
	/// <returns>
	/// The system language.
	/// </returns>
	public string GetSystemLanguage()
	{
		if(Application.systemLanguage == SystemLanguage.Unknown)
		{
			Debug.LogWarning("LanguageManager.cs: The system language of this application is Unknown");
			return "Unknown";
		}

		string systemLanguage = Application.systemLanguage.ToString();
#if !UNITY_WP8
		CultureInfo[] cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures);
		foreach(CultureInfo info in cultureInfos)
		{
			if(info.EnglishName == systemLanguage)
			{
				return info.Name;
			}
		}

		Debug.LogError("LanguageManager.cs: A system language of this application is could not be found!");
		return "System Language not found!";
#else
		Debug.LogError("LanguageManager.cs: GetSystemLanguage() Feature is not currently available in Smart Localization for Windows Phone 8");
		return defaultLanguage;
#endif
	}
	/// <summary>
	/// Gets the culture info of the specified string
	/// languageName = strings like "en", "es", "sv"
	/// </summary>
	public CultureInfo GetCultureInfo(string languageName)
	{
		return CultureInfo.GetCultureInfo(languageName);
	}
}
