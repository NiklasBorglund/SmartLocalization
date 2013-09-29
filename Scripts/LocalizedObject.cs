//
// LocalizedObject.cs
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

/// <summary>
/// The type of the localized object
/// </summary>
public enum LocalizedObjectType
{
	STRING = 0,
	GAME_OBJECT = 1,
	AUDIO = 2,
	TEXTURE = 3,
	INVALID = 4,
}

/// <summary>
/// The localized object, used to determine what type the string value from the .resx file is
/// </summary>
/// 
///
[System.Serializable]
public class LocalizedObject 
{
	/// <summary>
	/// The type of the string.
	/// </summary>
	/// 
	[SerializeField]
	private LocalizedObjectType objectType = LocalizedObjectType.STRING;
	/// <summary>
	/// Gets or sets the LocalizedObjectType of the object.
	/// </summary>
	/// <value>
	/// The LocalizedObjectType of the object.
	/// </value>
	public LocalizedObjectType ObjectType
	{
		get
		{
			return objectType;	
		}
		set
		{
			objectType = value;	
		}
	}
	
	/// <summary>
	/// The text value of the object, all objects are converted into string values in the end, whether it's a 
	/// file path or a simple string value
	/// </summary>
	[SerializeField]
	private string textValue;
	/// <summary>
	/// Gets or sets the text value.
	/// </summary>
	/// <value>
	/// The text value of the object, all objects are converted into string values in the end, whether it's a
	/// file path or a simple string value.
	/// </value>
	public string TextValue
	{
		get
		{
		 	return textValue;	
		}
		set
		{
			textValue = value;	
		}
	}
	
	/// <summary>
	/// Variable if this object type is GAME_OBJECT
	/// </summary>
	[SerializeField]
	private GameObject thisGameObject;
	/// <summary>
	/// Variable if this object type is GAME_OBJECT. Gets or sets the this game object.
	/// </summary>
	/// <value>
	/// The this game object.
	/// </value>
	public GameObject ThisGameObject
	{
		get
		{
			return thisGameObject;	
		}
		set
		{
			thisGameObject = value;	
		}
	}
	
	/// <summary>
	/// Variable if this object type is AUDIO
	/// </summary>
	[SerializeField]
	private AudioClip thisAudioClip;
	/// <summary>
	/// Variable if this object type is AUDIO. Gets or sets the this AudioClip.
	/// </summary>
	/// <value>
	/// This AudioClip.
	/// </value>
	public AudioClip ThisAudioClip
	{
		get
		{
			return thisAudioClip;	
		}
		set
		{
			thisAudioClip = value;	
		}
	}
	
	/// <summary>
	/// Variable if this object type is TEXTURE
	/// </summary>
	[SerializeField]
	private Texture thisTexture;
	/// <summary>
	/// Variable if this object type is TEXTURE. Gets or sets the this Texture.
	/// </summary>
	/// <value>
	/// This Texture.
	/// </value>
	public Texture ThisTexture
	{
		get
		{
			return thisTexture;	
		}
		set
		{
			thisTexture = value;	
		}
	}
	
	
	/// <summary>
	/// All keys that are not a string will begin with this identifier
	/// for example - if the localized object is an audio file, the key
	/// will begin with <type=AUDIO>
	/// regular text strings will not begin with any type of identifier
	/// </summary>
	public static string keyTypeIdentifier = "[type=";	
	/// <summary>
	/// Gets the LocalizedObjectType of the localized object.
	/// </summary>
	/// <returns>
	/// The LocalizedObjectType of the key value
	/// </returns>
	/// <param name='key'>
	/// The language key from the .resx file
	/// </param>
	public static LocalizedObjectType GetLocalizedObjectType(string key)
	{
		if(key.StartsWith(keyTypeIdentifier)) //Check if it is something else than a string value
		{
			if(key.StartsWith(keyTypeIdentifier + "AUDIO]")) // it's an audio file
			{
				return LocalizedObjectType.AUDIO;
			}
			else if(key.StartsWith(keyTypeIdentifier + "GAME_OBJECT]")) // it's a game object(prefab)
			{
				return LocalizedObjectType.GAME_OBJECT;
			}
			else if(key.StartsWith(keyTypeIdentifier + "TEXTURE]")) // it's a texture
			{
				return LocalizedObjectType.TEXTURE;
			}
			else //Something is wrong with the syntax
			{
				Debug.LogError("LocalizedObject.cs: ERROR IN SYNTAX of key:" + key + ", setting object type to STRING");
				return LocalizedObjectType.STRING;
			}
		}
		else //If not - it's a string value
		{
			return LocalizedObjectType.STRING;
		}
	}	
	/// <summary>
	/// Gets the clean key. i.e the key without a type identifier "<type=TEXTURE>" etc.
	/// </summary>
	/// <returns>
	/// The clean key without the type identifier
	/// </returns>
	/// <param name='key'>
	/// The key language value
	/// </param>
	/// <param name='objectType'>
	/// The LocalizedObjectType of the key
	/// </param>
	public static string GetCleanKey(string key, LocalizedObjectType objectType)
	{
		int identifierLength = (keyTypeIdentifier + objectType.ToString() + ">").Length;
		if(objectType == LocalizedObjectType.STRING)
		{
			return key;
		}
		else if(objectType == LocalizedObjectType.AUDIO ||
				objectType == LocalizedObjectType.GAME_OBJECT ||
				objectType == LocalizedObjectType.TEXTURE)
		{
			return key.Substring(identifierLength);	
		}
		//if none of the above, return the key and send error message
		Debug.LogError("LocalizedObject.GetCleanKey(key) error!, object type is unknown! objectType:" + (int)objectType);
		return key;
	}
	/// <summary>
	/// Gets the clean key. i.e the key without a type identifier "<type=TEXTURE>" etc.
	/// </summary>
	/// <returns>
	/// The clean key without the type identifier
	/// </returns>
	/// <param name='key'>
	/// The full key with the type identifier
	/// </param>
	public static string GetCleanKey(string key)
	{
		LocalizedObjectType objectType = GetLocalizedObjectType(key);
		return GetCleanKey(key, objectType);
	}
	/// <summary>
	/// Gets the full key value with identifiers and everything
	/// </summary>
	/// <returns>
	/// The full key.
	/// </returns>
	/// <param name='parsedKey'>
	/// Parsed key. (Clean key originally from GetCleanKey)
	/// </param>
	public string GetFullKey(string parsedKey)
	{
		if(objectType == LocalizedObjectType.STRING) 
		{
			//No identifier is returned for a string
			return parsedKey;	
		}
		
		return (keyTypeIdentifier + objectType.ToString() + "]" + parsedKey);
	}
	/// <summary>
	/// Gets the full key value with identifiers and everything
	/// </summary>
	/// <returns>
	/// The full key.
	/// </returns>
	/// <param name='parsedKey'>
	/// Parsed key. (Clean key originally from GetCleanKey)
	/// </param>
	public static string GetFullKey(string parsedKey, LocalizedObjectType objectType)
	{
		if(objectType == LocalizedObjectType.STRING) 
		{
			//No identifier is returned for a string
			return parsedKey;	
		}
		
		return (keyTypeIdentifier + objectType.ToString() + "]" + parsedKey);
	}
}
