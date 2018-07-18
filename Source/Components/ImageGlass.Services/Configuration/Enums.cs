﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2018 DUONG DIEU PHAP
Project homepage: http://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;

namespace ImageGlass.Services.Configuration
{
    /// <summary>
    /// The loading order list.
    /// **If we need to rename, have to update the language string too.
    /// Because the name is also language keyword!
    /// </summary>
    public enum ImageOrderBy
    {
        Name = 0,
        Length = 1,
        CreationTime = 2,
        Extension = 3,
        LastAccessTime = 4,
        LastWriteTime = 5,
        Random = 6
    }

    /// <summary>
    /// The list of Zoom Optimization.
    /// **If we need to rename, have to update the language string too.
    /// Because the name is also language keyword!
    /// </summary>
    public enum ZoomOptimizationMethods
    {
        Auto = 0,
        SmoothPixels = 1,
        ClearPixels = 2
    }

    public enum ImageFormatGroup
    {
        Default = 0,
        Optional = 1
    }

    public enum Constants
    {
        MENU_ICON_HEIGHT = 21,
        TOOLBAR_ICON_HEIGHT = 20,
        TOOLBAR_HEIGHT = 40
    }

    /// <summary>
    /// The list of mousewheel actions.
    /// **If we need to rename, have to update the language string too.
    /// Because the name is also language keyword!
    /// </summary>
    public enum MouseWheelActions
    {
        DoNothing = 0,
        Zoom = 1,
        ScrollVertically = 2,
        ScrollHorizontally = 3,
        BrowseImages = 4
    }

    /// <summary>
    /// Define the flags to tell frmMain update the UI
    /// </summary>
    [Flags]
    public enum MainFormForceUpdateAction
    {
        NONE = 0,
        COLOR_PICKER_MENU = 1,
        THEME = 2,
        LANGUAGE = 4,
        THUMBNAIL_BAR = 8,
        THUMBNAIL_ITEMS = 16,
        TOOLBAR = 32,
        IMAGE_LIST = 64,
        IMAGE_FOLDER = 128,
        OTHER_SETTINGS = 256
    }


    /// <summary>
    /// The list of layout mode.
    /// **If we need to rename, have to update the language string too.
    /// Because the name is also language keyword!
    /// </summary>
    public enum LayoutMode
    {
        Standard = 0,
        Designer = 1
    }
}
