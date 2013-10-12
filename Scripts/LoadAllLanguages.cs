//
//  LoadAllLanguages.cs
//
// This class will load all languages and show all the keys/values
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
using System.Globalization;

public class LoadAllLanguages : MonoBehaviour 
{
	private Dictionary<string,string> currentLanguageValues;
	private LanguageManager thisLanguageManager;
	private Vector2 valuesScrollPosition = Vector2.zero;
	private Vector2 languagesScrollPosition = Vector2.zero;

	void Start () 
	{
		thisLanguageManager = LanguageManager.Instance;
		
		string systemLanguage = thisLanguageManager.GetSystemLanguage();
		if(thisLanguageManager.IsLanguageSupported(systemLanguage))
		{
			thisLanguageManager.ChangeLanguage(systemLanguage);	
		}
		
		if(thisLanguageManager.AvailableLanguages.Count > 0)
		{
			currentLanguageValues = thisLanguageManager.GetTextDataBase();	
		}
		else
		{
			Debug.LogError("No languages are created!, Open the Smart Localization plugin at Window->Smart Localization and create your language!");
		}
	}
	
	void OnGUI() 
	{
		if(thisLanguageManager.IsInitialized)
		{
			GUILayout.Label("Current Language:" + thisLanguageManager.language);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Keys:", GUILayout.Width(460));
			GUILayout.Label("Values:", GUILayout.Width(460));
			GUILayout.EndHorizontal();
			
			valuesScrollPosition = GUILayout.BeginScrollView(valuesScrollPosition);
			foreach(KeyValuePair<string,string> languageValue in currentLanguageValues)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(languageValue.Key, GUILayout.Width(460));
				GUILayout.Label(languageValue.Value, GUILayout.Width(460));
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
			
			languagesScrollPosition = GUILayout.BeginScrollView (languagesScrollPosition);
#if !UNITY_WP8
			foreach(CultureInfo language in thisLanguageManager.AvailableLanguagesCultureInfo)
			{
				if(GUILayout.Button(language.NativeName, GUILayout.Width(960)))
				{
					thisLanguageManager.ChangeLanguage(language.Name);
					currentLanguageValues = thisLanguageManager.GetTextDataBase();
				}
			}
#else
			foreach(string language in thisLanguageManager.AvailableLanguages)
			{
				if(GUILayout.Button(language, GUILayout.Width(960)))
				{
					thisLanguageManager.ChangeLanguage(language);
					currentLanguageValues = thisLanguageManager.GetTextDataBase();
				}
			}
#endif
			GUILayout.EndScrollView();
		}
	}
}
