//
//  CustomResxImporter.cs
//
// Creates or rewrites a .txt file for each .resx file in a subfolder called 
// GeneratedAssets whenever the .resx changes
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

using UnityEditor;
using UnityEngine;
using System.IO;

public class CustomResxImporter : AssetPostprocessor 
{
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string asset in importedAssets)
        {
            if (asset.EndsWith(".resx"))
            {
                string filePath = asset.Substring(0, asset.Length - Path.GetFileName(asset).Length) + "Generated Assets/";
                string newFileName = filePath + Path.GetFileNameWithoutExtension(asset) + ".txt";

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
				
				//Delete the file if it already exists
				if(File.Exists(newFileName))
				{
					File.Delete(newFileName);	
				}

                StreamReader reader = new StreamReader(asset);
                string fileData = reader.ReadToEnd();
                reader.Close();

                FileStream resourceFile = new FileStream(newFileName, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(resourceFile);
                writer.Write(fileData);
                writer.Close();
                resourceFile.Close();

                AssetDatabase.Refresh(ImportAssetOptions.Default);
            }
        }
    }

}
