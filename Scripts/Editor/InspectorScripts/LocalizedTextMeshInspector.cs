//
// LocalizedTextMeshInspector.cs
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
using UnityEditor;

[CustomEditor(typeof(TextMesh))]
public class LocalizedTextMeshInspector : Editor
{
    private string selectedKey = null;

    void Awake()
    {
        LocalizedTextMesh textObject = ((TextMesh)target).gameObject.GetComponent<LocalizedTextMesh>();
        if(textObject != null)
        {
            selectedKey = textObject.localizedKey;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        selectedKey = LocalizedKeySelector.SelectKeyGUI(selectedKey, true, LocalizedObjectType.STRING);

        if(!Application.isPlaying && GUILayout.Button("Use Key", GUILayout.Width(70)))
        {
            GameObject targetGameObject = ((TextMesh)target).gameObject;
            LocalizedTextMesh textObject = targetGameObject.GetComponent<LocalizedTextMesh>();
            if(textObject == null)
            {
                textObject = targetGameObject.AddComponent<LocalizedTextMesh>();
            }

            textObject.localizedKey = selectedKey;
        }
    }
}
