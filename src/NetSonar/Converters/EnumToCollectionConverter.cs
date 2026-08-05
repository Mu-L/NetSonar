/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Data.Converters;
using NetSonar.Avalonia.Extensions;
using NetSonar.Avalonia.Models;

namespace NetSonar.Avalonia.Converters;

public class EnumToCollectionConverter : IValueConverter
{
    private readonly Dictionary<(Type EnumType, bool OrderByName), LocalizedEnumValues> _collections = [];

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not Enum) return null;
        var orderByName = System.Convert.ToBoolean(parameter);
        var key = (value.GetType(), orderByName);
        if (_collections.TryGetValue(key, out var collection)) return collection;

        collection = new LocalizedEnumValues(key.Item1, orderByName);
        _collections.Add(key, collection);
        return collection;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return null;
        //string parameterString = parameter.ToString();
        //return Enum.Parse(targetType, parameterString);
    }

    private sealed class LocalizedEnumValues : ObservableCollection<ValueDescription>
    {
        public LocalizedEnumValues(Type enumType, bool orderByName)
        {
            foreach (var value in EnumExtensions.GetAllValues(enumType, orderByName))
            {
                Add(new ValueDescription(value, value.GetDescription()));
            }

            App.Localization.PropertyChanged += LocalizationOnPropertyChanged;
        }

        private void LocalizationOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(App.Localization.Culture)) return;

            foreach (var item in Items)
            {
                if (item.Value is Enum value) item.Description = value.GetDescription();
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
