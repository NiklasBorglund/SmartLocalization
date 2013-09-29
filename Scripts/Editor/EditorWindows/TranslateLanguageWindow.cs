//
//  TranslateLanguageWindow.cs
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
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;

public class TranslateLanguageWindow : EditorWindow
{
#region Members
	[SerializeField]
	private Dictionary<string,LocalizedObject> rootValues;	
	[SerializeField]
	private List<SerializableLocalizationObjectPair> thisLanguageValues = new List<SerializableLocalizationObjectPair>();	
	[SerializeField]
	private string thisLanguage;
	[SerializeField]
	private CultureInfo thisCultureInfo;
	/// <summary> A bool that is set if the root file have been changed </summary>
	[SerializeField]
	private bool rootFileChanged = false;
	/// <summary> The scroll view position </summary>
	[SerializeField]
	private Vector2 scrollPosition = Vector2.zero;
	/// <summary> Did the GUI change? </summary>
	[SerializeField]
	private bool guiChanged = false;
	[SerializeField]
	///<summary> is the language available for translation?</summary>
	private bool canLanguageBeTranslated = false;
	///<summary> Languages available to translate from</summary>
	private List<CultureInfo> availableTranslateFromLanguages = new List<CultureInfo>();
	///<summary> For the translateFromLanguage from popup</summary>
	private string[] availableTranslateLangEnglishNames;
	///<summary> Popup index for translateFromLanguage</summary>
	private int translateFromLanguageValue = 0;
	private int oldTranslateFromLanguageValue = 0;
	private string translateFromLanguage = "None";
	///<summary> The language dictionary to translate from</summary>
	private Dictionary<string,LocalizedObject> translateFromDictionary;
	private string searchText = "";
	private HOEditorUndoManager undoManager;

	private SmartLocalizationWindow smartLocWindow;
#endregion
#region Initialization
	public void Initialize(CultureInfo thisCultureInfo, bool checkTranslation = false)
	{
		if(smartLocWindow != null && !Application.isPlaying && thisCultureInfo != null)
		{
			if(undoManager == null)
			{
				// Instantiate Undo Manager
				undoManager = new HOEditorUndoManager(this, "Smart Localization - Translate Language Window");
			}

			if(thisCultureInfo != null)
			{
				bool newLanguage = thisCultureInfo != this.thisCultureInfo ? true : false;
				this.thisCultureInfo = thisCultureInfo;
				if(thisLanguageValues == null || thisLanguageValues.Count < 1 || newLanguage)
				{
					InitializeLanguage(thisCultureInfo, LocFileUtility.LoadParsedLanguageFile(null), LocFileUtility.LoadParsedLanguageFile(thisCultureInfo.Name));
				}
			}

			if(checkTranslation)
			{
				//Check if the language can be translated
				canLanguageBeTranslated = false;
				CheckIfCanBeTranslated();

				if(translateFromDictionary != null)
				{
					translateFromDictionary.Clear();
					translateFromDictionary = null;
				}
			}
		}
	}
	/// <summary>
	/// Initializes the Language
	/// </summary>
	void InitializeLanguage(CultureInfo info, Dictionary<string, LocalizedObject> rootValues, Dictionary<string, LocalizedObject> thisLanguageValues)
	{
		this.rootValues = rootValues;	
		this.thisLanguageValues.Clear();
		this.thisLanguageValues = LocFileUtility.CreateSerializableLocalizationList(thisLanguageValues);
		//Load assets
		LocFileUtility.LoadAllAssets(this.thisLanguageValues);

		this.thisLanguage = (thisCultureInfo.EnglishName + " - " + thisCultureInfo.Name);
		rootFileChanged = false;
	}
#endregion
#region EditorWindow Overrides
	void OnEnable()
	{
		EditRootLanguageFileWindow.OnRootFileChanged += OnRootFileChanged;
		Initialize(thisCultureInfo);
	}

	void OnDisable()
	{
		EditRootLanguageFileWindow.OnRootFileChanged -= OnRootFileChanged;
	}

	void OnProjectChange()
	{
		Initialize(thisCultureInfo);
	}
	
	void OnFocus()
	{	
		Initialize(thisCultureInfo, true);
	}

	void OnGUI()
	{
		if(EditorWindowUtility.ShowWindow())
		{
			if(smartLocWindow == null || thisCultureInfo == null) 
			{
				this.Close();// Temp fix
			}
			else if(!rootFileChanged)
			{
				undoManager.CheckUndo();
				GUILayout.Label("Language - " + thisLanguage, EditorStyles.boldLabel, GUILayout.Width(200));

				//Copy all the Base Values
				GUILayout.Label("If you want to copy all the base values from the root file", EditorStyles.miniLabel);
				if(GUILayout.Button("Copy All Base Values", GUILayout.Width(150)))
				{
					int count = 0;
					foreach(KeyValuePair<string,LocalizedObject> rootValue in rootValues)
					{
						if(rootValue.Value.ObjectType == LocalizedObjectType.STRING)
						{
							thisLanguageValues[count].changedValue.TextValue = rootValue.Value.TextValue;
						}
						count++;
					}
				}

				GUILayout.Label("Microsoft Translator", EditorStyles.boldLabel);
				if(!smartLocWindow.MicrosoftTranslator.IsInitialized)
				{
					GUILayout.Label("Microsoft Translator is not authenticated", EditorStyles.miniLabel);
				}
				else
				{
					if(canLanguageBeTranslated)
					{
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("Translate From:", GUILayout.Width(100));
						translateFromLanguageValue = EditorGUILayout.Popup(translateFromLanguageValue, availableTranslateLangEnglishNames);
						EditorGUILayout.EndHorizontal();

						if(oldTranslateFromLanguageValue != translateFromLanguageValue)
						{
							oldTranslateFromLanguageValue = translateFromLanguageValue;
							//The value have been changed, load the language file of the other language that you want to translate from
							//I load it like this to show the translate buttons only on the ones that can be translated i.e some values
							//in the "from" language could be an empty string - no use in translating that
							if(translateFromDictionary != null)
							{
								translateFromDictionary.Clear();
								translateFromDictionary = null;
							}
							if(translateFromLanguageValue != 0)
							{
								string englishName = availableTranslateLangEnglishNames[translateFromLanguageValue];
								foreach(CultureInfo info in availableTranslateFromLanguages)
								{
									if(info.EnglishName == englishName)
									{
										translateFromDictionary = LocFileUtility.LoadParsedLanguageFile(info.Name);
										translateFromLanguage = info.Name;
										break;
									}
								}
							}
						}

						//Translate all the available keys
						if(translateFromLanguageValue != 0 && GUILayout.Button("Translate all text", GUILayout.Width(150)))
						{
							List<string> keys = new List<string>();
							List<string> textsToTranslate = new List<string>();
							int characterCount = 0;
							foreach(KeyValuePair<string,LocalizedObject> stringPair in translateFromDictionary)
							{
								if(stringPair.Value.ObjectType == LocalizedObjectType.STRING &&
									stringPair.Value.TextValue != null && stringPair.Value.TextValue != "")
								{
									int textLength = stringPair.Value.TextValue.Length;
									//Microsoft translator only support translations below 1000 character
									//I'll cap it to 700, which gives 300 extra if the translated value is longer
									if(textLength < 700)
									{
										characterCount += textLength;
										keys.Add(stringPair.Key);
										textsToTranslate.Add(stringPair.Value.TextValue);
									}
								}

								//Microsoft translator only support translations with 100 array values and a total
								// character cap of 10000,
								// I'll cap it to 7000, which gives 3000 extra if the translated value is longer
								if(keys.Count >= 99 || characterCount >= 7000)
								{
									//Create a new reference to the list with keys, because we need it non-cleared in the callback
									List<string> keysToSend = new List<string>();
									keysToSend.AddRange(keysToSend.ToArray());

									//Send the values
									smartLocWindow.MicrosoftTranslator.TranslateArray(textsToTranslate, translateFromLanguage,
										thisCultureInfo.Name, keysToSend, new TranslateCompleteArrayCallback(TranslatedTextArrayCompleted));

									//Reset values
									characterCount = 0;
									keys.Clear();
									textsToTranslate.Clear();
								}
							}
							if(keys.Count != 0)
							{
								smartLocWindow.MicrosoftTranslator.TranslateArray(textsToTranslate, translateFromLanguage,
									thisCultureInfo.Name, keys, new TranslateCompleteArrayCallback(TranslatedTextArrayCompleted));

								//Reset values
								characterCount = 0;
								keys.Clear();
								textsToTranslate.Clear();
							}
						}
					}
					else
					{
						GUILayout.Label(thisCultureInfo.EnglishName + " is not available for translation", EditorStyles.miniLabel);
					}
				}

				GUILayout.Label("Language Values", EditorStyles.boldLabel);
				//Search field
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Search for Key:", GUILayout.Width(100));
				searchText = EditorGUILayout.TextField(searchText);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.Width(120));
				GUILayout.Label("Base Value", EditorStyles.boldLabel, GUILayout.Width(120));
				GUILayout.Label("Copy Base", EditorStyles.miniLabel, GUILayout.Width(70));
				if(canLanguageBeTranslated)
				{
					//TODO::Change to small picture
					GUILayout.Label("T", EditorStyles.miniLabel, GUILayout.Width(20));
				}
				GUILayout.Label(thisLanguage + " Value", EditorStyles.boldLabel);
				EditorGUILayout.EndHorizontal();

				//Check if the user searched for a value
				bool didSearch = false;
				if(searchText != "")
				{
					didSearch = true;
					GUILayout.Label("Search Results - \"" + searchText + "\":", EditorStyles.boldLabel);
				}

				//Start the scroll view
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				int iterationCount = 0;
				foreach(KeyValuePair<string,LocalizedObject> rootValue in rootValues)
				{
					if(didSearch)
					{
						//If the name of the key doesn't contain the search value, then skip a value
						if(!rootValue.Key.ToLower().Contains(searchText.ToLower()))
						{
							continue;
						}
					}

					if(rootValue.Value.ObjectType == LocalizedObjectType.STRING)
					{
						OnTextFieldGUI(rootValue, iterationCount);
					}
					else if(rootValue.Value.ObjectType == LocalizedObjectType.AUDIO)
					{
						OnAudioGUI(rootValue, iterationCount);
					}
					else if(rootValue.Value.ObjectType == LocalizedObjectType.GAME_OBJECT)
					{
						OnGameObjectGUI(rootValue, iterationCount);
					}
					else if(rootValue.Value.ObjectType == LocalizedObjectType.TEXTURE)
					{
						OnTextureGUI(rootValue, iterationCount);
					}

					iterationCount++;
				}
				//End the scroll view
				EditorGUILayout.EndScrollView();

				if(guiChanged)
				{
					GUILayout.Label("- You have unsaved changes", EditorStyles.miniLabel);
				}

				//If any changes to the gui is made
				if(GUI.changed)
				{
					guiChanged = true;
				}

				GUILayout.Label("Save Changes", EditorStyles.boldLabel);
				GUILayout.Label("Remember to always press save when you have changed values", EditorStyles.miniLabel);
				if(GUILayout.Button("Save/Rebuild"))
				{
					//Copy everything into a dictionary
					Dictionary<string,string> newLanguageValues = new Dictionary<string, string>();
					foreach(SerializableLocalizationObjectPair objectPair in this.thisLanguageValues)
					{
						if(objectPair.changedValue.ObjectType == LocalizedObjectType.STRING)
						{
							newLanguageValues.Add(objectPair.changedValue.GetFullKey(objectPair.keyValue), objectPair.changedValue.TextValue);
						}
						else
						{
							//Delete the file in case there was a file there previously
							LocFileUtility.DeleteFileFromResources(objectPair.changedValue.GetFullKey(objectPair.keyValue), thisCultureInfo);

							//Store the path to the file
							string pathValue = LocFileUtility.CopyFileIntoResources(objectPair, thisCultureInfo);
							newLanguageValues.Add(objectPair.changedValue.GetFullKey(objectPair.keyValue), pathValue);
						}
					}
					LocFileUtility.SaveLanguageFile(newLanguageValues, LocFileUtility.rootLanguageFilePath + "." + thisCultureInfo.Name + LocFileUtility.resXFileEnding);
					guiChanged = false;
				}

				undoManager.CheckDirty();
			}
			else
			{
				//The root file did change, which means that you have to reload. A key might have changed
				//We can't have language files with different keys
				GUILayout.Label("The root file might have changed", EditorStyles.boldLabel);
				GUILayout.Label("The root file did save, which means that you have to reload. A key might have changed.", EditorStyles.miniLabel);
				GUILayout.Label("You can't have language files with different keys", EditorStyles.miniLabel);
				if(GUILayout.Button("Reload Language File"))
				{
					InitializeLanguage(thisCultureInfo, LocFileUtility.LoadParsedLanguageFile(null), LocFileUtility.LoadParsedLanguageFile(thisCultureInfo.Name));
				}
			}
		}
	}
#endregion
#region Translation Check
	/// <summary>
	/// Checks if this language can be translated by Microsoft Translator
	/// </summary>
	private void CheckIfCanBeTranslated()
	{
		availableTranslateFromLanguages.Clear();
		//Clear the array
		if(availableTranslateLangEnglishNames != null)
		{
			Array.Clear(availableTranslateLangEnglishNames, 0, availableTranslateLangEnglishNames.Length);
			availableTranslateLangEnglishNames = null;
		}
		
		if(translateFromDictionary != null)
		{
			translateFromDictionary.Clear();
			translateFromDictionary = null;
		}
		translateFromLanguageValue = 0;
		oldTranslateFromLanguageValue = 0;
		//Create a list that will store the english names
		List<string> englishNames = new List<string>();
		englishNames.Add("None");
		if(smartLocWindow.MicrosoftTranslator.IsInitialized)
		{
			if(smartLocWindow.MicrosoftTranslator.LanguagesAvailableForTranslation.Contains(thisCultureInfo.Name))
			{
				canLanguageBeTranslated = true;
				foreach(CultureInfo cultureInfo in smartLocWindow.AvailableLanguages)
				{
					if(cultureInfo != thisCultureInfo && smartLocWindow.MicrosoftTranslator.LanguagesAvailableForTranslation.Contains(cultureInfo.Name))
					{
						availableTranslateFromLanguages.Add(cultureInfo);
						englishNames.Add(cultureInfo.EnglishName);
					}
				}
			}
			else
			{
				canLanguageBeTranslated = false;
			}
		}
		availableTranslateLangEnglishNames = englishNames.ToArray();
	}
#endregion
#region GUI Functions
	/// <summary>
	/// Shows the GUI for a key with the type of STRING
	/// </summary>
	/// <param name='rootValue'>
	/// Root value.
	/// </param>
	private void OnTextFieldGUI(KeyValuePair<string,LocalizedObject> rootValue, int iterationCount)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(rootValue.Key, GUILayout.Width(120));
		GUILayout.Label(rootValue.Value.TextValue,GUILayout.Width(120));
				
		//If the user wants to copy the base value of this key
		if(GUILayout.Button("Copy Base", GUILayout.Width(70)))
		{
			thisLanguageValues[iterationCount].changedValue = rootValue.Value;
		}
		//If the language can be translated
		if(canLanguageBeTranslated)
		{
			if(translateFromDictionary != null && translateFromDictionary[rootValue.Key].TextValue != null &&
				translateFromDictionary[rootValue.Key].TextValue != "")
			{
				if(GUILayout.Button("T", GUILayout.Width(20)))
				{
					smartLocWindow.MicrosoftTranslator.TranslateText(translateFromDictionary[rootValue.Key].TextValue,translateFromLanguage, thisCultureInfo.Name,
					rootValue.Key, new TranslateCompleteCallback(TranslatedTextCompleted));
				}
			}
			else
			{
				GUILayout.Label("T", GUILayout.Width(20));
			}
		}
		
		thisLanguageValues[iterationCount].changedValue.TextValue = EditorGUILayout.TextField(thisLanguageValues[iterationCount].changedValue.TextValue);
		EditorGUILayout.EndHorizontal();
	}
	/// <summary>
	/// Shows the GUI for a key with the type of GAME_OBJECT
	/// </summary>
	/// <param name='rootValue'>
	/// Root value.
	/// </param>
	private void OnGameObjectGUI(KeyValuePair<string,LocalizedObject> rootValue, int iterationCount)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(rootValue.Key, GUILayout.Width(120));
		GUILayout.Label(rootValue.Value.TextValue,GUILayout.Width(120));
				
		//If the user wants to copy the base value of this key
		GUILayout.Label("Copy Base", GUILayout.Width(70));
		
		//If the language can be translated(PlaceHolder for now)
		GUILayout.Label("T", GUILayout.Width(20));

		
		thisLanguageValues[iterationCount].changedValue.ThisGameObject = (GameObject)EditorGUILayout.ObjectField(
																		thisLanguageValues[iterationCount].changedValue.ThisGameObject, 
																		typeof(GameObject),false);
		EditorGUILayout.EndHorizontal();
	}
	/// <summary>
	/// Shows the GUI for a key with the type of AUDIO
	/// </summary>
	/// <param name='rootValue'>
	/// Root value.
	/// </param>
	private void OnAudioGUI(KeyValuePair<string,LocalizedObject> rootValue, int iterationCount)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(rootValue.Key, GUILayout.Width(120));
		GUILayout.Label(rootValue.Value.TextValue,GUILayout.Width(120));
				
		//If the user wants to copy the base value of this key
		GUILayout.Label("Copy Base", GUILayout.Width(70));
		
		//If the language can be translated(PlaceHolder for now)
		GUILayout.Label("T", GUILayout.Width(20));

		
		thisLanguageValues[iterationCount].changedValue.ThisAudioClip = (AudioClip)EditorGUILayout.ObjectField(
																		thisLanguageValues[iterationCount].changedValue.ThisAudioClip, 
																		typeof(AudioClip),false);
		EditorGUILayout.EndHorizontal();
	}
	/// <summary>
	/// Shows the GUI for a key with the type of TEXTURE
	/// </summary>
	/// <param name='rootValue'>
	/// Root value.
	/// </param>
	private void OnTextureGUI(KeyValuePair<string,LocalizedObject> rootValue, int iterationCount)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(rootValue.Key, GUILayout.Width(120));
		GUILayout.Label(rootValue.Value.TextValue,GUILayout.Width(120));
				
		//If the user wants to copy the base value of this key
		GUILayout.Label("Copy Base", GUILayout.Width(70));
		
		//If the language can be translated(PlaceHolder for now)
		GUILayout.Label("T", GUILayout.Width(20));

		
		thisLanguageValues[iterationCount].changedValue.ThisTexture = (Texture)EditorGUILayout.ObjectField(
																		thisLanguageValues[iterationCount].changedValue.ThisTexture, 
																		typeof(Texture),false);
		EditorGUILayout.EndHorizontal();
	}
#endregion
#region Event Callbacks

	void OnRootFileChanged()
	{
		rootFileChanged = true;
	}
	/// <summary>
	/// Callback when the translated text is translated
	/// </summary>
	/// <param name='key'>
	/// The key of the translated value
	/// </param>
	/// <param name='translatedValue'>
	/// Translated value.
	/// </param>
	public void TranslatedTextCompleted(string key, string translatedValue)
	{
		for(int i = 0; i < thisLanguageValues.Count; i++)
		{
			SerializableLocalizationObjectPair objectPair = thisLanguageValues[i];
			if(objectPair.keyValue == key)
			{
				objectPair.changedValue.TextValue = translatedValue;
				break;
			}
		}
	}
	/// <summary>
	/// Callback when the translated array of texts is completed
	/// </summary>
	/// <param name='keys'>
	/// Keys.
	/// </param>
	/// <param name='translatedValues'>
	/// Translated values.
	/// </param>
	public void TranslatedTextArrayCompleted(List<string> keys, List<string> translatedValues)
	{	
		for(int j = 0; j < keys.Count; j++)
		{
			for(int i = 0; i < thisLanguageValues.Count; i++)
			{
				SerializableLocalizationObjectPair objectPair = thisLanguageValues[i];
				if(objectPair.keyValue == keys[j])
				{
					objectPair.changedValue.TextValue = translatedValues[j];
					break;
				}
			}
		}
	}
#endregion
	/// <summary>
	/// Shows the translate window window.
	/// </summary>
    public static TranslateLanguageWindow ShowWindow(CultureInfo info, SmartLocalizationWindow smartLocWindow)
    {
		TranslateLanguageWindow thisWindow = (TranslateLanguageWindow)EditorWindow.GetWindow<TranslateLanguageWindow>("Translate Language",true,typeof(SmartLocalizationWindow));
		thisWindow.smartLocWindow = smartLocWindow;
		thisWindow.Initialize(info);
		return thisWindow;
    }
}
