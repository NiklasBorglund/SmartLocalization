//
// EditRootLanguageFileWindow.cs
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
using System;

/// <summary>
/// Serializable string pair. Helper class for a string pair that is serializeable(for the undo functionality)
/// </summary>
[System.Serializable]
public class SerializableStringPair
{
	/// <summary>
	/// The original value.
	/// </summary>
	public string originalValue;
	/// <summary>
	/// The changed value.
	/// </summary>
	public string changedValue;
	/// <summary>
	/// Initializes a new instance of the <see cref="SerializableStringPair"/> class.
	/// </summary>
	/// <param name='originalValue'>
	/// Original value.
	/// </param>
	/// <param name='changedValue'>
	/// Changed value.
	/// </param>
	public SerializableStringPair(string originalValue, string changedValue)
	{
		this.originalValue = originalValue;
		this.changedValue = changedValue;
	}
}
/// <summary>
/// Serializable localization object pair. Helper class for a string pair that is serializeable(for the undo functionality)
/// </summary>
[System.Serializable]
public class SerializableLocalizationObjectPair
{
	public string keyValue;
	public LocalizedObject changedValue;
	/// <summary>
	/// Initializes a new instance of the <see cref="SerializableLocalizationObjectPair"/> class.
	/// </summary>
	/// <param name='keyValue'>
	/// Key value.
	/// </param>
	/// <param name='changedValue'>
	/// Changed value.
	/// </param>
	public SerializableLocalizationObjectPair(string keyValue, LocalizedObject changedValue)
	{
		this.keyValue = keyValue;
		this.changedValue = changedValue;
	}
}

public class EditRootLanguageFileWindow : EditorWindow
{
#region Static Events
	public static Action OnRootFileChanged = null;
#endregion

#region Members
	/// <summary>Containing the original keys and the changes to them, if any.</summary>
	[SerializeField]
	private List<SerializableStringPair> changedRootKeys = new List<SerializableStringPair>();
	/// <summary>Containing the original values and any changes to them</summary>
	[SerializeField]
	private List<SerializableLocalizationObjectPair> changedRootValues = new List<SerializableLocalizationObjectPair>();	
	/// <summary>The scroll view position</summary>
	[SerializeField]
	private Vector2 scrollPosition = Vector2.zero;
	/// <summary>Did the GUI change?</summary>
	[SerializeField]
	private bool guiChanged = false;
	/// <summary>Search field.</summary>
	[SerializeField]
	private string searchText = "";
	/// <summary>The Undo Manager</summary>
	private HOEditorUndoManager undoManager;
	/// <summary>The parsed root values. This is used to check root key duplicates</summary>
	[SerializeField]
	private Dictionary<string, LocalizedObject> parsedRootValues = new Dictionary<string, LocalizedObject>();
	/// <summary>The key types</summary>
	[SerializeField]
	private string[] keyTypes;
#endregion
#region Initialization
	void Initialize()
	{
		if(undoManager == null)
		{
			// Instantiate Undo Manager
			undoManager = new HOEditorUndoManager(this, "Smart Localization - Edit Root Language Window");
		}
		if(keyTypes == null)
		{
			//Get the different key types
			keyTypes = Enum.GetNames(typeof(LocalizedObjectType));
		}

		if(changedRootKeys == null)
		{
			changedRootKeys = new List<SerializableStringPair>();
		}
		if(changedRootValues == null)
		{
			changedRootValues = new List<SerializableLocalizationObjectPair>();
		}
		if(parsedRootValues == null)
		{
			parsedRootValues = new Dictionary<string, LocalizedObject>();
		}

		if(parsedRootValues.Count < 1)
		{
			SetRootValues(LocFileUtility.LoadParsedLanguageFile(null));
		}
	}
	/// <summary>
	/// Sets the dictionaries necessary to change them within the extension
	/// </summary>
	/// <param name='rootValues'>
	/// Root values.
	/// </param>
	public void SetRootValues(Dictionary<string, LocalizedObject> rootValues)
	{
		changedRootValues.Clear();
		changedRootKeys.Clear();
		parsedRootValues.Clear();

		foreach(KeyValuePair<string,LocalizedObject> rootValue in rootValues)
		{
			changedRootKeys.Add(new SerializableStringPair(rootValue.Key, rootValue.Key));
			changedRootValues.Add(new SerializableLocalizationObjectPair(rootValue.Key, rootValue.Value));

			LocalizedObject copyObject = new LocalizedObject();
			copyObject.ObjectType = rootValue.Value.ObjectType;
			copyObject.TextValue = rootValue.Value.TextValue;
			parsedRootValues.Add(rootValue.Key, copyObject);
		}

		searchText = "";
	}
#endregion

#region Editor Window Overrides

	void OnEnable()
	{
		Initialize();
	}

	void OnFocus()
	{
		Initialize();
	}

	void OnProjectChange()
	{
		Initialize();
	}

	void OnGUI()
	{
		if(EditorWindowUtility.ShowWindow())
		{
			undoManager.CheckUndo();
			GUILayout.Label ("Root Values", EditorStyles.boldLabel);
			
			//Search field
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label ("Search for Key:", GUILayout.Width(100));
			searchText = EditorGUILayout.TextField(searchText);
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Key Type", GUILayout.Width(100));
			GUILayout.Label("Key");
			GUILayout.Label("Base Value/Comment");
			GUILayout.Label("Delete", EditorStyles.miniLabel, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();
			
			//Create the scroll view
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			
			//Delete key information
			bool deleteKey = false;
			int indexToDelete = 0;
			
			//Check if the user searched for a value
			bool didSearch = false;
			if(searchText != "")
			{
				didSearch = true;
				GUILayout.Label ("Search Results - \"" + searchText + "\":", EditorStyles.boldLabel);
			}
			
			for(int i = 0; i < changedRootKeys.Count; i++)
			{
				SerializableStringPair rootValue = changedRootKeys[i];
				if(didSearch)
				{
					//If the name of the key doesn't contain the search value, then skip a value
					if(!rootValue.originalValue.ToLower().Contains(searchText.ToLower()))
					{
						continue;	
					}
				}
				EditorGUILayout.BeginHorizontal();
				
				//Popup of all the different key values
				changedRootValues[i].changedValue.ObjectType = (LocalizedObjectType)EditorGUILayout.Popup((int)changedRootValues[i].changedValue.ObjectType, 
																											keyTypes, GUILayout.Width(100));
			
				rootValue.changedValue = EditorGUILayout.TextField(rootValue.changedValue);
				changedRootValues[i].changedValue.TextValue = EditorGUILayout.TextField(changedRootValues[i].changedValue.TextValue);
				if(GUILayout.Button("Delete", GUILayout.Width(50)))
				{
					deleteKey = true;
					indexToDelete = i;
				}
				
				EditorGUILayout.EndHorizontal();
			}
			
			//End the scrollview
			EditorGUILayout.EndScrollView();
			
			if(GUILayout.Button("Add New Key"))
			{
				AddNewKey();
			}
			
			//Delete the key outside the foreach loop
			if(deleteKey)
			{
				DeleteKey(indexToDelete);
			}
			
			if(guiChanged)
			{
				GUILayout.Label ("- You have unsaved changes", EditorStyles.miniLabel);
			}
			
			//If any changes to the gui is made
			if(GUI.changed)
			{
				guiChanged = true;
			}
			
			GUILayout.Label ("Save Changes", EditorStyles.boldLabel);
			GUILayout.Label ("Remember to always press save when you have changed values", EditorStyles.miniLabel);
			if(GUILayout.Button("Save Root Language File"))
			{
				Dictionary<string,string> changeNewRootKeys = new Dictionary<string, string>();
				Dictionary<string,string> changeNewRootValues = new Dictionary<string, string>();
				
				for(int i = 0; i < changedRootKeys.Count; i++)
				{
					SerializableStringPair rootKey = changedRootKeys[i];
					SerializableLocalizationObjectPair rootValue = changedRootValues[i];
					//Check for possible duplicates and rename them
					string newKeyValue = LocFileUtility.AddNewKeyPersistent(changeNewRootKeys, rootKey.originalValue, rootValue.changedValue.GetFullKey(rootKey.changedValue));
					
					//Check for possible duplicates and rename them(same as above)
					LocFileUtility.AddNewKeyPersistent(changeNewRootValues, newKeyValue, rootValue.changedValue.TextValue);
				}
			
				//Add the full values before saving
				Dictionary<string,string> changeNewRootKeysToSave = new Dictionary<string, string>();
				Dictionary<string,string> changeNewRootValuesToSave = new Dictionary<string, string>();
			
				foreach(KeyValuePair<string,string> rootKey in changeNewRootKeys)
				{
					LocalizedObject thisLocalizedObject = parsedRootValues[rootKey.Key];
					changeNewRootKeysToSave.Add(thisLocalizedObject.GetFullKey(rootKey.Key), rootKey.Value);
					changeNewRootValuesToSave.Add(thisLocalizedObject.GetFullKey(rootKey.Key), changeNewRootValues[rootKey.Key]);
				}
			
				LocFileUtility.SaveRootLanguageFile(changeNewRootKeysToSave, changeNewRootValuesToSave);

				//Fire the root language changed event
				if(OnRootFileChanged != null)
				{
					OnRootFileChanged();
				}
			
				//Reload the window(in case of duplicate keys)
				SetRootValues(LocFileUtility.LoadParsedLanguageFile(null));
				guiChanged = false;
			}
			
			undoManager.CheckDirty();
		}
	}
#endregion
#region Add/Delete Keys
	/// <summary>
	/// Adds a new key to the dictionary
	/// </summary>
	private void AddNewKey()
	{
		LocalizedObject dummyObject = new LocalizedObject();
		dummyObject.ObjectType = LocalizedObjectType.STRING;
		dummyObject.TextValue = "New Value";

		string addedKey = LocFileUtility.AddNewKeyPersistent(parsedRootValues, "New Key", dummyObject);

		LocalizedObject copyObject = new LocalizedObject();
		copyObject.ObjectType = LocalizedObjectType.STRING;
		copyObject.TextValue = "New Value";
		changedRootKeys.Add(new SerializableStringPair(addedKey, addedKey));
		changedRootValues.Add(new SerializableLocalizationObjectPair(addedKey, copyObject));
	}
	private void DeleteKey(int index)
	{
		parsedRootValues.Remove(changedRootKeys[index].originalValue);
		changedRootKeys.RemoveAt(index);
		changedRootValues.RemoveAt(index);
	}
#endregion
#region Show Window
    public static EditRootLanguageFileWindow ShowWindow()
    {
		EditRootLanguageFileWindow thisWindow = (EditRootLanguageFileWindow)EditorWindow.GetWindow<EditRootLanguageFileWindow>
			("Edit Root Language File",true,typeof(SmartLocalizationWindow));

		return thisWindow;
	}
#endregion
}
