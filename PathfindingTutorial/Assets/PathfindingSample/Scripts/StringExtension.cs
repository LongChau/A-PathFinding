using System;
using UnityEngine;

public static class StringExtension
{

	// This needs to be added to a public static class to be used like an extension
	public static void CopyToClipboard(this string s)
	{
		TextEditor te = new TextEditor();
		te.text = s;
		te.SelectAll();
		te.Copy();
	}
}
