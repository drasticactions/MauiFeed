﻿// <copyright file="BooleanToVisibilityConverter.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MauiFeed.WinUI.Tools
{
    /// <summary>
    /// Boolean to Visibility Converter.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not bool realValue)
            {
                return Visibility.Collapsed;
            }

            return realValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
