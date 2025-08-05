// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;


/// <summary>
/// Represents a single menu item with text and selection state.
/// </summary>
public class MenuItem<T> where T : System.Enum
{
    public string Text { get; set; }
    public bool IsSelected { get; set; }
    public T Status { get; set; }
    public Action Action { get; internal set; }

    public MenuItem(string text, T status = default)
    {
        Text = text;
        Status = status;
    }
}
