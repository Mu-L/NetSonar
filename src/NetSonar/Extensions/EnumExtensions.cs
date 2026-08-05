/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using NetSonar.Avalonia.Models;

namespace NetSonar.Avalonia.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var localizationKey = $"Enum.{value.GetType().Name}.{value}";
        var localizedDescription = App.Localization.GetString(localizationKey);
        if (localizedDescription != localizationKey) return localizedDescription;

        var attributes = value.GetType()
            .GetField(value.ToString())?
            .GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes is not null && attributes.Length > 0)
        {
            if (attributes[0] is DescriptionAttribute attr) return attr.Description;
        }

        // If no description is found, the least we can do is replace underscores with spaces
        // You can add your own custom default formatting logic here
        var ti = CultureInfo.CurrentCulture.TextInfo;
        return ti.ToTitleCase(ti.ToLower(value.ToString().Replace("_", " ")));
    }

    public static IEnumerable<Enum> GetAllValues(Type t, bool orderByName = false)
    {
        if (!t.IsEnum)
            throw new ArgumentException($"{nameof(t)} must be an enum type");

        return orderByName
            ? Enum.GetValues(t).Cast<Enum>().OrderBy(e => e?.ToString(), StringComparer.Ordinal)
            : Enum.GetValues(t).Cast<Enum>();
    }

    public static IEnumerable<TEnum> GetAllValues<TEnum>(bool orderByName = false) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        return orderByName
            ? values.OrderBy(e => e.ToString(), StringComparer.Ordinal)
            : values;
    }

    public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t, bool orderByName = false)
    {
        return GetAllValues(t, orderByName).Select(e => new ValueDescription(e, e.GetDescription()));
    }

    /// <summary>
    /// Gets all the set flags of an enum value(s).
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="flags"></param>
    /// <returns></returns>
    /// <remarks>For enums with <see cref="FlagsAttribute"/>.<br/>
    /// If you have an enum value set to 0 it will always return. (Filter it before or after calling this function)<br/>
    /// If you have negative value it can return all flags as undesired effect.</remarks>
    public static IEnumerable<TEnum> GetSetFlags<TEnum>(this TEnum flags) where TEnum : struct, Enum
    {
        var flagsInt64 = Convert.ToInt64(flags);

        foreach (var flag in Enum.GetValues<TEnum>())
        {
            var flagInt64 = Convert.ToInt64(flag);
            if ((flagsInt64 & flagInt64) == flagInt64)
            {
                yield return flag;
            }
        }
    }

    /// <summary>
    /// Gets all the set flags of an enum value(s).
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="flags"></param>
    /// <param name="ignoreFlag">Set a flag to exclude from returning, eg: If you want to ignore or remove an always return 0 flag.</param>
    /// <returns></returns>
    /// <remarks>For enums with <see cref="FlagsAttribute"/>.<br/>
    /// If you have an enum value set to 0 it will always return. (Filter it before or after calling this function)<br/>
    /// If you have negative value it can return all flags as undesired effect.</remarks>
    public static IEnumerable<TEnum> GetSetFlagsIgnoring<TEnum>(this TEnum flags, TEnum ignoreFlag) where TEnum : struct, Enum
    {
        var flagsInt64 = Convert.ToInt64(flags);
        var ignoreFlagInt64 = Convert.ToInt64(ignoreFlag);

        flagsInt64 &= ~ignoreFlagInt64;

        foreach (var flag in Enum.GetValues<TEnum>())
        {
            var flagInt64 = Convert.ToInt64(flag);
            if ((flagsInt64 & flagInt64) == flagInt64)
            {
                yield return flag;
            }
        }
    }

    /// <summary>
    /// Gets all the set flags of an enum value(s).
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="flags"></param>
    /// <param name="ignoreFlags">Set flag(s) to exclude from returning, eg: If you want to ignore or remove an always return 0 flag.</param>
    /// <returns></returns>
    /// <remarks>For enums with <see cref="FlagsAttribute"/>.<br/>
    /// If you have an enum value set to 0 it will always return. (Filter it before or after calling this function)<br/>
    /// If you have negative value it can return all flags as undesired effect.</remarks>
    public static IEnumerable<TEnum> GetSetFlagsIgnoring<TEnum>(this TEnum flags, params IEnumerable<TEnum> ignoreFlags) where TEnum : struct, Enum
    {
        var flagsInt64 = Convert.ToInt64(flags);

        foreach (var ignoreFlag in ignoreFlags)
        {
            var ignoreFlagInt64 = Convert.ToInt64(ignoreFlag);
            flagsInt64 &= ~ignoreFlagInt64;
        }

        foreach (var flag in Enum.GetValues<TEnum>())
        {
            var flagInt64 = Convert.ToInt64(flag);
            if ((flagsInt64 & flagInt64) == flagInt64)
            {
                yield return flag;
            }
        }
    }
}
