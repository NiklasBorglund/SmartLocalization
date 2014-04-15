//
// LocalizedAudioSource.cs
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

public class LocalizedAudioSource : MonoBehaviour 
{
	public string localizedKey = "INSERT_KEY_HERE";
	public AudioClip thisAudioClip;
	private AudioSource thisAudioSource;
	
    private LanguageManager thisLanguageManager;

	void Start () 
	{
		//Subscribe to the change language event
        thisLanguageManager = LanguageManager.Instance;
        thisLanguageManager.addLanguageChangedListener(OnChangeLanguage);
		
		//Get the audio source
		thisAudioSource = this.audio;
		
		//Run the method one first time
		OnChangeLanguage(thisLanguageManager);
	}
	
    void OnDestroy()
    {
        thisLanguageManager.removeLanguageChangedListener(OnChangeLanguage);
    }

	void OnChangeLanguage(LanguageManager thisLanguageManager)
	{
		//Initialize all your language specific variables here
		thisAudioClip = thisLanguageManager.GetAudioClip(localizedKey);
		
		if(thisAudioSource != null)
		{
			thisAudioSource.clip = thisAudioClip;
		}
	}
}
