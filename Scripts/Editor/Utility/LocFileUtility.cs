//
// LocFileUtility.cs
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
using System.IO;
using UnityEditor;
using System.Globalization;
using System.Xml;
using System.Text;

public class LocFileUtility
{
#region Static Members
	public static string rootLanguageFilePath = Application.dataPath + "/SmartLocalizationLanguage/Resources/Localization/Language";
	public static string resXFileEnding = ".resx";
#endregion
#region File Operations
	/// <summary>
	/// Gets the file extension for the file at the specified path
	/// </summary>
	/// <param name='fileName'>
	/// The file name without the extension. If the full name is for example hello.png, this parameter
	/// should be only "hello"
	/// </param>
	/// <param name='relativeFolderPath'>
	/// The relative path to the folder containing the asset file
	/// relativeFolderPath should be relative to the project folder. Like: "Assets/MyTextures/".
	/// </param>
	public static string GetFileExtension(string fileName, string relativeFolderPath)
	{
		string fullFolderPath = Application.dataPath + relativeFolderPath;
		
		if(!Directory.Exists(fullFolderPath))
		{
			return null;
		}
		string[] assetsInFolder = Directory.GetFiles(fullFolderPath);
		
		foreach(string asset in assetsInFolder)
		{
			if(!asset.EndsWith(".meta")) //If this is not a .meta file
			{
				string currentFileName = Path.GetFileNameWithoutExtension(asset);
				if(fileName == currentFileName)
				{
					return Path.GetExtension(asset);
				}
			}
		}
		
		return null;
	}
	/// <summary>
	/// Gets the file extension from the full filePath
	/// </summary>
	/// <returns>
	/// The file extension.
	/// </returns>
	/// <param name='fullFilePath'>
	/// Full file path.
	/// </param>
	public static string GetFileExtension(string fullFilePath)
	{
		return Path.GetExtension(fullFilePath);
	}
	/// <summary>
	/// Checks if the directory exist the and creates it if it doesn't
	/// </summary>
	public static void CheckAndCreateDirectory(string path)
	{
		if(!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}
	/// <summary>
	/// Checks if root language file exists.
	/// </summary>
	public static bool CheckIfRootLanguageFileExists()
	{
		if(File.Exists(rootLanguageFilePath + resXFileEnding))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	/// <summary>
	/// Checks if file exists.(Full filepath)
	/// </summary>
	/// <returns>
	/// The if file exists.
	/// </returns>
	/// <param name='filePath'>
	/// If set to <c>true</c> file path.
	/// </param>
	public static bool CheckIfFileExists(string filePath)
	{
		if(File.Exists(filePath))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	/// <summary>
	/// Checks if language file exists.
	/// </summary>
	/// <returns>
	/// The if language file exists.
	/// </returns>
	/// <param name='language'>
	/// If set to <c>true</c> language.
	/// </param>
	public static bool CheckIfLanguageFileExists(string language)
	{
		if(File.Exists(rootLanguageFilePath + "." + language + resXFileEnding))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	/// <summary>
	/// Checks the available languages.
	/// </summary>
	public static void CheckAvailableLanguages(List<CultureInfo> availableLanguages, List<CultureInfo> notAvailableLanguages, List<string> notAvailableLanguagesEnglishNames)
	{
		availableLanguages.Clear();
		notAvailableLanguages.Clear();
		notAvailableLanguagesEnglishNames.Clear();

		CultureInfo[] cultureInfos = CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
		foreach(CultureInfo info in cultureInfos)
		{
			if(LocFileUtility.CheckIfLanguageFileExists(info.Name))
			{
				availableLanguages.Add(info);
			}
			else
			{
				notAvailableLanguages.Add(info);
				notAvailableLanguagesEnglishNames.Add(info.EnglishName);
			}
		}
		notAvailableLanguagesEnglishNames.Sort();
	}
	/// <summary>
	/// Returns a list with all the available languages
	/// </summary>
	/// <returns></returns>
	public static List<CultureInfo> GetAvailableLanguages()
	{
		List<CultureInfo> availableLanguages = new List<CultureInfo>();
		CultureInfo[] cultureInfos = CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
		foreach(CultureInfo info in cultureInfos)
		{
			if(LocFileUtility.CheckIfLanguageFileExists(info.Name))
			{
				availableLanguages.Add(info);
			}
		}
		return availableLanguages;
	}
	/// <summary>
	/// Saves the root language file and updates all the available languages.
	/// </summary>
	public static void SaveRootLanguageFile(Dictionary<string,string> changedRootKeys, Dictionary<string,string> changedRootValues)
	{
		//The dictionary with all the final changes
		Dictionary<string,string> changedDictionary = new Dictionary<string, string>();

		foreach(KeyValuePair<string,string> changedKey in changedRootKeys)
		{
			if(changedKey.Key == changedKey.Value)
			{
				//The key is not changed, just add the key and the changed value to the new dictionary
				AddNewKeyPersistent(changedDictionary, changedKey.Key, changedRootValues[changedKey.Key]);
			}
			else
			{
				//Add the new key along with the new changed value
				AddNewKeyPersistent(changedDictionary, changedKey.Value, changedRootValues[changedKey.Key]);
			}
		}

		//Look if any keys were deleted,(so that we can delete the created files)
		//(Somewhat costly operation)
		List<string> deletedKeys = new List<string>();
		IEnumerable<string> originalKeys = LoadLanguageFile(null).Keys;
		foreach(string originalKey in originalKeys)
		{
			bool foundMatch = false;
			foreach(KeyValuePair<string,string> changedKey in changedRootKeys)
			{
				if(originalKey == changedKey.Key)
				{
					foundMatch = true;
					break;
				}
			}
			if(!foundMatch)
			{
				deletedKeys.Add(originalKey);
			}
		}

		//Save the language file
		SaveLanguageFile(changedDictionary, LocFileUtility.rootLanguageFilePath + LocFileUtility.resXFileEnding);

		//Change all the key values for all the translated languages
		Dictionary<string,string> changedCultureValues = new Dictionary<string, string>();
		List<CultureInfo> availableLanguages = GetAvailableLanguages();
		foreach(CultureInfo cultureInfo in availableLanguages)
		{
			Dictionary<string,string> currentCultureValues = LocFileUtility.LoadLanguageFile(cultureInfo.Name);
			foreach(KeyValuePair<string,string> changedKey in changedRootKeys)
			{
				string thisValue;
				currentCultureValues.TryGetValue(changedKey.Key, out thisValue);
				if(thisValue == null)
				{
					thisValue = "";
				}

				//If the key is changed, we need to change the asset names as well
				if(changedKey.Key != changedKey.Value && thisValue != "")
				{
					LocalizedObjectType originalType = LocalizedObject.GetLocalizedObjectType(changedKey.Key);
					LocalizedObjectType changedType = LocalizedObject.GetLocalizedObjectType(changedKey.Value);

					if(originalType != changedType)
					{
						//If the type is changed, then delete the asset and reset the value
						DeleteFileFromResources(changedKey.Key, cultureInfo);
						thisValue = "";
					}
					else
					{
						//just rename it otherwise
						RenameFileFromResources(changedKey.Key, changedKey.Value, cultureInfo);
					}
				}

				AddNewKeyPersistent(changedCultureValues, changedKey.Value, thisValue);
			}

			//Save the language file
			SaveLanguageFile(changedCultureValues, LocFileUtility.rootLanguageFilePath + "." + cultureInfo.Name + LocFileUtility.resXFileEnding);
			changedCultureValues.Clear();

			//Remove all the deleted files associated with the deleted keys
			foreach(string deletedKey in deletedKeys)
			{
				DeleteFileFromResources(deletedKey, cultureInfo);
			}
		}
	}
#endregion
#region Language Loading / Saving / Deleting
	/// <summary>
	/// Loads the parsed language file.(without the type identifiers)
	/// </summary>
	/// <returns>
	/// The parsed language file.
	/// </returns>
	/// <param name='languageCode'>
	/// Language code.
	/// </param>
	public static Dictionary<string, LocalizedObject> LoadParsedLanguageFile(string languageCode)
	{
		LanguageManager thisManager = LanguageManager.Instance;
		thisManager.ChangeLanguage (languageCode);
		Dictionary<string,LocalizedObject> languageDataBase = thisManager.GetLocalizedObjectDataBase();
		thisManager.Clear ();
		
		return languageDataBase;	
	}
	/// <summary>
	/// Loads the language file and returns the RAW values
	/// </summary>
	public static Dictionary<string,string> LoadLanguageFile(string languageCode)
	{
		LanguageManager thisManager = LanguageManager.Instance;
		thisManager.ChangeLanguage (languageCode);
		Dictionary<string,string> languageDataBase = thisManager.GetTextDataBase();
		thisManager.Clear ();
		
		return languageDataBase;
	}
	/// <summary>
	/// Saves a language file(.resx) at the specified path containing the values in the languageValueDictionary
	/// </summary>
	/// <param name='languageValueDictionary'>
	/// Language value dictionary.
	/// </param>
	/// <param name='filePath'>
	/// File path.
	/// </param>
	public static void SaveLanguageFile(Dictionary<string, string> languageValueDictionary, string filePath)
	{
		//Get resx header
		TextAsset emptyResourceFile = Resources.Load("EmptyResourceHeader") as TextAsset;
		string resxHeader = emptyResourceFile.text;

		//Create the new language file and copy all the base values
		FileStream resourceFile = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		StreamWriter writer = new StreamWriter(resourceFile);

		XmlWriter xmlWriter = XmlWriter.Create(writer);
		xmlWriter.Settings.Encoding = Encoding.UTF8;
		xmlWriter.Settings.Indent = true;
		xmlWriter.WriteStartDocument();
		xmlWriter.WriteStartElement("root");
		xmlWriter.WriteRaw(resxHeader); // Paste in the raw resx header
		xmlWriter.WriteString("\n");  

		//Copy the keys over to the new language
		foreach(KeyValuePair<string, string> keyPair in languageValueDictionary)
		{
			xmlWriter.WriteString("\t");
			xmlWriter.WriteStartElement("data");
			xmlWriter.WriteAttributeString("name", keyPair.Key);
			xmlWriter.WriteAttributeString("xml:space", "preserve");
			xmlWriter.WriteString("\n\t\t");

			xmlWriter.WriteStartElement("value");
			xmlWriter.WriteString(keyPair.Value);
			xmlWriter.WriteEndElement(); //value
			xmlWriter.WriteString("\n\t");
			xmlWriter.WriteEndElement(); //data
			xmlWriter.WriteString("\n");  
		}

		xmlWriter.WriteEndElement(); //root
		xmlWriter.WriteEndDocument();

		xmlWriter.Close();
		writer.Close();
		resourceFile.Close();

		//Update the assetfolders
		AssetDatabase.Refresh(ImportAssetOptions.Default);
	}
	/// <summary>
	/// Creates the root resource file.
	/// </summary>
	public static void CreateRootResourceFile()
	{
		//Check if the localizationfolders exists, if not - create them.
		string localizationPath = Application.dataPath + "/SmartLocalizationLanguage/";
		LocFileUtility.CheckAndCreateDirectory(localizationPath);
		localizationPath += "Resources/";
		LocFileUtility.CheckAndCreateDirectory(localizationPath);
		localizationPath += "Localization/";
		LocFileUtility.CheckAndCreateDirectory(localizationPath);

		//Add a dummy value so that the user will see how everything works
		Dictionary<string,string> baseDictionary = new Dictionary<string, string>();
		baseDictionary.Add("MyFirst.Key", "MyFirstValue");

		LocFileUtility.SaveLanguageFile(baseDictionary, LocFileUtility.rootLanguageFilePath + LocFileUtility.resXFileEnding);
	}
	/// <summary>
	/// Creates a new language file with the keys from the root file.
	/// </summary>
	/// <param name='languageName'>
	/// The name of the language
	/// </param>
	public static void CreateNewLanguage(string languageName)
	{
		Dictionary<string,string> rootValues = LocFileUtility.LoadLanguageFile(null);

		//Copy the keys over to the new language
		Dictionary<string,string> baseDictionary = new Dictionary<string, string>();
		foreach(KeyValuePair<string, string> keyPair in rootValues)
		{
			baseDictionary.Add(keyPair.Key, "");
		}

		//Save the new language file
		LocFileUtility.SaveLanguageFile(baseDictionary, LocFileUtility.rootLanguageFilePath + "." + languageName + LocFileUtility.resXFileEnding);
	}
	/// <summary>
	/// Deletes the language.
	/// </summary>
	/// <param name='cultureInfo'>
	/// Culture info.
	/// </param>
	public static void DeleteLanguage(CultureInfo cultureInfo)
	{
		string filePath = LocFileUtility.rootLanguageFilePath + "." + cultureInfo.Name + LocFileUtility.resXFileEnding;
		if(File.Exists(filePath))
		{
			File.Delete(filePath);
			File.Delete(filePath + ".meta");
		}
		//The text file
		filePath = Application.dataPath + "/SmartLocalizationLanguage/Resources/Localization/Generated Assets/Language." + cultureInfo.Name + ".txt";
		if(File.Exists(filePath))
		{
			File.Delete(filePath);
			File.Delete(filePath + ".meta");
		}

		//The assets directory
		filePath = Application.dataPath + "/SmartLocalizationLanguage/Resources/Localization/" + cultureInfo.Name;
		if(Directory.Exists(filePath))
		{
			Directory.Delete(filePath + "/", true);
			File.Delete(filePath + ".meta");
		}
		AssetDatabase.Refresh();
	}
	/// <summary>
	/// Creates the serializable localization list from the parsed LocalizedObjects
	/// </summary>
	/// <returns>
	/// The serializable localization list.
	/// </returns>
	/// <param name='languageValues'>
	/// Language values.
	/// </param>
	public static List<SerializableLocalizationObjectPair> CreateSerializableLocalizationList(Dictionary<string, LocalizedObject> languageValues)
	{
		List<SerializableLocalizationObjectPair> localizationList = new List<SerializableLocalizationObjectPair>();
		foreach(KeyValuePair<string,LocalizedObject> languageValue in languageValues)
		{
			localizationList.Add(new SerializableLocalizationObjectPair(languageValue.Key, languageValue.Value));
		}
		return localizationList;
	}
#endregion
#region Asset Loading / Moving / Deleting
	/// <summary>
	/// Loads all assets in language values if they have a valid file path
	/// </summary>
	public static void LoadAllAssets(List<SerializableLocalizationObjectPair> thisLanguageValues)
	{
		foreach(SerializableLocalizationObjectPair objectPair in thisLanguageValues)
		{
			if(objectPair.changedValue.ObjectType == LocalizedObjectType.AUDIO && 
				objectPair.changedValue.TextValue != null && objectPair.changedValue.TextValue != "")
			{
				objectPair.changedValue.ThisAudioClip = AssetDatabase.LoadAssetAtPath(
														AssetDatabase.GUIDToAssetPath(objectPair.changedValue.TextValue), typeof(AudioClip)) as AudioClip;
			}
			else if(objectPair.changedValue.ObjectType == LocalizedObjectType.GAME_OBJECT && 
				objectPair.changedValue.TextValue != null && objectPair.changedValue.TextValue != "")
			{
				objectPair.changedValue.ThisGameObject = AssetDatabase.LoadAssetAtPath(
														AssetDatabase.GUIDToAssetPath(objectPair.changedValue.TextValue), typeof(GameObject)) as GameObject;
			}
			else if(objectPair.changedValue.ObjectType == LocalizedObjectType.TEXTURE && 
				objectPair.changedValue.TextValue != null && objectPair.changedValue.TextValue != "")
			{
				objectPair.changedValue.ThisTexture = AssetDatabase.LoadAssetAtPath(
														AssetDatabase.GUIDToAssetPath(objectPair.changedValue.TextValue), typeof(Texture)) as Texture;
			}
		}
	}
	/// <summary>
	/// Copies the file into the resources folder. Naming the new asset to
	/// KEY
	/// </summary>
	/// <returns>
	/// The file into resources.
	/// </returns>
	/// <param name='objectPair'>
	/// Object pair.
	/// </param>
	public static string CopyFileIntoResources(SerializableLocalizationObjectPair objectPair, CultureInfo thisCultureInfo)
	{
		string languageFolderPath = Application.dataPath + "/SmartLocalizationLanguage/Resources/Localization/";
		LocFileUtility.CheckAndCreateDirectory(languageFolderPath + thisCultureInfo.Name + "/");

		//TODO: Create generic function
		LocalizedObject objectToCopy = objectPair.changedValue;
		if(objectToCopy.ObjectType == LocalizedObjectType.AUDIO && objectToCopy.ThisAudioClip != null)
		{
			string thisFilePath = languageFolderPath + thisCultureInfo.Name + "/Audio Files/";
			string newFileName = objectPair.keyValue;

			LocFileUtility.CheckAndCreateDirectory(thisFilePath);

			//Get the current path of the object
			string currentAssetPath = AssetDatabase.GetAssetPath(objectToCopy.ThisAudioClip);

			//Get the fileExtension of the asset
			string fileExtension = LocFileUtility.GetFileExtension(Application.dataPath + currentAssetPath);

			//Copy or replace the file to the new path
			FileUtil.ReplaceFile(currentAssetPath, thisFilePath + newFileName + fileExtension);

			return AssetDatabase.AssetPathToGUID(currentAssetPath);
		}
		else if(objectToCopy.ObjectType == LocalizedObjectType.TEXTURE && objectToCopy.ThisTexture != null)
		{
			string thisFilePath = languageFolderPath + thisCultureInfo.Name + "/Textures/";
			string newFileName = objectPair.keyValue;

			LocFileUtility.CheckAndCreateDirectory(thisFilePath);
			string currentAssetPath = AssetDatabase.GetAssetPath(objectToCopy.ThisTexture);

			string fileExtension = LocFileUtility.GetFileExtension(Application.dataPath + currentAssetPath);

			FileUtil.ReplaceFile(currentAssetPath, thisFilePath + newFileName + fileExtension);

			return AssetDatabase.AssetPathToGUID(currentAssetPath);
		}
		else if(objectToCopy.ObjectType == LocalizedObjectType.GAME_OBJECT && objectToCopy.ThisGameObject != null)
		{
			string thisFilePath = languageFolderPath + thisCultureInfo.Name + "/Prefabs/";
			string newFileName = objectPair.keyValue;

			LocFileUtility.CheckAndCreateDirectory(thisFilePath);
			string currentAssetPath = AssetDatabase.GetAssetPath(objectToCopy.ThisGameObject);

			string fileExtension = LocFileUtility.GetFileExtension(Application.dataPath + currentAssetPath);

			FileUtil.ReplaceFile(currentAssetPath, thisFilePath + newFileName + fileExtension);

			return AssetDatabase.AssetPathToGUID(currentAssetPath);
		}
		else
		{
			return "";
		}
	}
	/// <summary>
	/// Deletes the localized file from resources.
	/// </summary>
	/// <param name='key'>
	/// Key.
	/// </param>
	/// <param name='thisCultureInfo'>
	/// This culture info.
	/// </param>
	public static void DeleteFileFromResources(string key, CultureInfo thisCultureInfo)
	{
		string languageFolderPath = "/SmartLocalizationLanguage/Resources/Localization/" + thisCultureInfo.Name;
		LocalizedObjectType thisKeyType = LocalizedObject.GetLocalizedObjectType(key);
		string cleanKey = LocalizedObject.GetCleanKey(key);

		if(thisKeyType == LocalizedObjectType.GAME_OBJECT)
		{
			languageFolderPath += "/Prefabs/" + cleanKey + ".prefab";
			if(CheckIfFileExists(Application.dataPath + languageFolderPath))
			{
				AssetDatabase.DeleteAsset("Assets" + languageFolderPath);
			}

		}
		else if(thisKeyType == LocalizedObjectType.AUDIO)
		{
			languageFolderPath += "/Audio Files/";
			string fileExtension = GetFileExtension(cleanKey, languageFolderPath);
			languageFolderPath += cleanKey + fileExtension;

			if(CheckIfFileExists(Application.dataPath + languageFolderPath))
			{
				AssetDatabase.DeleteAsset("Assets" + languageFolderPath);
			}
		}
		else if(thisKeyType == LocalizedObjectType.TEXTURE)
		{
			languageFolderPath += "/Textures/";
			string fileExtension = GetFileExtension(cleanKey, languageFolderPath);
			languageFolderPath += cleanKey + fileExtension;

			if(CheckIfFileExists(Application.dataPath + languageFolderPath))
			{
				AssetDatabase.DeleteAsset("Assets" + languageFolderPath);
			}
		}
	}
	/// <summary>
	/// Renames the localized file from resources.
	/// </summary>
	/// <param name='key'>
	/// Key.
	/// </param>
	/// <param name='thisCultureInfo'>
	/// This culture info.
	/// </param>
	public static void RenameFileFromResources(string key, string newKey, CultureInfo thisCultureInfo)
	{
		string languageFolderPath = "/SmartLocalizationLanguage/Resources/Localization/" + thisCultureInfo.Name;
		LocalizedObjectType thisKeyType = LocalizedObject.GetLocalizedObjectType(key);
		string cleanKey = LocalizedObject.GetCleanKey(key);
		string cleanNewKey = LocalizedObject.GetCleanKey(newKey);

		if(thisKeyType == LocalizedObjectType.GAME_OBJECT)
		{
			languageFolderPath += "/Prefabs/" + cleanKey + ".prefab";
			if(CheckIfFileExists(Application.dataPath + languageFolderPath))
			{
				AssetDatabase.RenameAsset("Assets" + languageFolderPath, cleanNewKey);
			}
		}
		else if(thisKeyType == LocalizedObjectType.AUDIO)
		{
			languageFolderPath += "/Audio Files/";
			string fileExtension = GetFileExtension(cleanKey, languageFolderPath);
			languageFolderPath += cleanKey + fileExtension;

			if(CheckIfFileExists(Application.dataPath + languageFolderPath))
			{
				AssetDatabase.RenameAsset("Assets" + languageFolderPath, cleanNewKey);
			}
		}
		else if(thisKeyType == LocalizedObjectType.TEXTURE)
		{
			languageFolderPath += "/Textures/";
			string fileExtension = GetFileExtension(cleanKey, languageFolderPath);
			languageFolderPath += cleanKey + fileExtension;
			if(CheckIfFileExists(Application.dataPath + languageFolderPath))
			{
				AssetDatabase.RenameAsset("Assets" + languageFolderPath, cleanNewKey);
			}
		}
		AssetDatabase.Refresh();
	}
#endregion
#region Dictionary Helper Methods
	/// <summary>
	/// Adds a new key to a dictionary<string,string> and does not stop until a unique key is found
	/// </summary>
	public static string AddNewKeyPersistent(Dictionary<string,string> thisDictionary, string desiredKey, string newValue)
	{
		LocalizedObjectType thisKeyType = LocalizedObject.GetLocalizedObjectType(desiredKey);
		//Clean the key from unwanted type identifiers
		//Nothing will happen to a regular string, since a string doesn't have an identifier
		desiredKey = LocalizedObject.GetCleanKey(desiredKey, thisKeyType);

		if(!thisDictionary.ContainsKey(desiredKey) && thisKeyType == LocalizedObjectType.STRING)
		{
			thisDictionary.Add(desiredKey, newValue);
			return desiredKey;
		}
		else
		{
			bool newKeyFound = false;
			int count = 0;
			string newKeyName = desiredKey;
			while(!newKeyFound)
			{
				if(!thisDictionary.ContainsKey(newKeyName))
				{
					bool duplicateFound = false;
					foreach(KeyValuePair<string,string> stringPair in thisDictionary)
					{
						string cleanKey = LocalizedObject.GetCleanKey(stringPair.Key);
						if(cleanKey == newKeyName)
						{
							duplicateFound = true;
							break;
						}
					}
					if(!duplicateFound)
					{
						thisDictionary.Add(LocalizedObject.GetFullKey(newKeyName, thisKeyType), newValue);
						newKeyFound = true;
						return desiredKey;
					}
					else
					{
						newKeyName = desiredKey + count;
						count++;
					}
				}
				else
				{
					newKeyName = desiredKey + count;
					count++;
				}
			}
			Debug.Log("Duplicate keys in dictionary was found! - renaming key:" + desiredKey + " to:" + newKeyName);
			return newKeyName;
		}
	}
	/// <summary>
	/// Adds a new key to a dictionary<string,LocalizedObject> and does not stop until a unique key is found
	/// </summary>
	public static string AddNewKeyPersistent(Dictionary<string, LocalizedObject> thisDictionary, string desiredKey, LocalizedObject newValue)
	{
		if(!thisDictionary.ContainsKey(desiredKey))
		{
			thisDictionary.Add(desiredKey, newValue);
			return desiredKey;
		}
		else
		{
			bool newKeyFound = false;
			int count = 0;
			while(!newKeyFound)
			{
				if(!thisDictionary.ContainsKey(desiredKey + count))
				{
					thisDictionary.Add(desiredKey + count, newValue);
					newKeyFound = true;
				}
				else
				{
					count++;
				}
			}
			Debug.LogWarning("Duplicate keys in dictionary was found! - renaming key:" + desiredKey + " to:" + (desiredKey + count));
			return (desiredKey + count);
		}
	}
#endregion
}
