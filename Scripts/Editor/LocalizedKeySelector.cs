//
// LocalizedKeySelector.cs
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

public class LocalizedKeySelector 
{
	private static List<string> parsedRootValues = new List<string>();
	private static LocalizedObjectType loadedObjectType;
	public static bool autoRefresh = false;

	/// <summary>
	/// Call this from OnInspectorGUI in your own editor class
	/// </summary>
	/// <returns>
	/// The selected key.
	/// </returns>
	/// <param name='currentKey'>
	/// Current key.
	/// </param>
	/// <param name='sort'>
	/// Set this to true if you only want to show keys of a specific type
	/// </param>
	/// <param name='sortType'>
	/// Sort type.
	/// </param>
	public static string SelectKeyGUI(string currentKey, bool sort = false, LocalizedObjectType sortType = LocalizedObjectType.INVALID)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Smart Localization",EditorStyles.boldLabel);
		if (GUILayout.Button("Open", GUILayout.Width(50)))
        {
			SmartLocalizationWindow.ShowWindow();
		}
		EditorGUILayout.EndHorizontal();
		
		if(autoRefresh || parsedRootValues.Count == 0 || sortType != loadedObjectType)
		{
			RefreshList(sort, sortType);
		}
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Sort Mode: ",EditorStyles.miniLabel, GUILayout.Width(55));
		if(sort)
		{
			GUILayout.Label(sortType.ToString() + " only.",EditorStyles.miniLabel);
		}
		else
		{
			GUILayout.Label("Showing ALL types",EditorStyles.miniLabel);
		}
		EditorGUILayout.EndHorizontal();
		
		if(LocFileUtility.CheckIfRootLanguageFileExists() && !Application.isPlaying)
		{
			int index = parsedRootValues.IndexOf(currentKey);
			index = Mathf.Max(0, index);
			int newIndex = index;
			newIndex = EditorGUILayout.Popup(index, parsedRootValues.ToArray());
			
			if(newIndex != index)
			{
				currentKey = parsedRootValues[newIndex];
			}
			
			if(!autoRefresh && GUILayout.Button("Refresh list", GUILayout.Width(80)))
			{
				RefreshList(sort, sortType);
			}
		}
		else if(Application.isPlaying)
		{
			GUILayout.Label("Feature not available in play mode.",EditorStyles.miniLabel);
		}
		else
		{
			GUILayout.Label("There is no Smart Localization system created",EditorStyles.miniLabel);
			//There is no language created
			if (GUILayout.Button("Create New Localization System"))
        	{
				LocFileUtility.CreateRootResourceFile();
			}
		}
		
		return currentKey;
	}
	
	public static void RefreshList(bool sort, LocalizedObjectType sortType)
	{
		if(!Application.isPlaying)
		{
			parsedRootValues.Clear();
	
			Dictionary<string, LocalizedObject> values = LocFileUtility.LoadParsedLanguageFile(null);
			if(sort)
			{
				loadedObjectType = sortType;
				foreach(KeyValuePair<string, LocalizedObject> pair in values)
				{
					if(pair.Value.ObjectType == sortType)
					{
						parsedRootValues.Add(pair.Key);	
					}
				}
			}
			else
			{
				//Use invalid for showing all
				loadedObjectType = LocalizedObjectType.INVALID;
				
				parsedRootValues.AddRange(values.Keys);
			}
			
			if(parsedRootValues.Count > 0)
			{
				parsedRootValues.Insert(0,"--- No key selected ---");
			}
			else
			{
				parsedRootValues.Add("--- No localized keys available ---");
			}
		}
	}
}
