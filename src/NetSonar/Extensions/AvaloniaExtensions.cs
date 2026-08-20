using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using NetSonar.Avalonia.SystemOS;
using ZLinq;

namespace NetSonar.Avalonia.Extensions;

public static class AvaloniaExtensions
{
    public static FilePickerFileType[] FilePickerJson { get; } =
    [
        new("JSON files")
        {
            Patterns = ["*.json"],
            AppleUniformTypeIdentifiers = ["public.plain-text"],
            MimeTypes = ["text/json"]
        }
    ];

    public static FilePickerFileType[] FilePickerCsv { get; } =
    [
        new("CSV files")
        {
            Patterns = ["*.csv"],
            AppleUniformTypeIdentifiers = ["public.plain-text"],
            MimeTypes = ["text/csv"]
        }
    ];

    public static FilePickerFileType[] FilePickerTsv { get; } =
    [
        new("TSV files")
        {
            Patterns = ["*.tsv"],
            AppleUniformTypeIdentifiers = ["public.plain-text"],
            MimeTypes = ["text/tab-separated-values"]
        }
    ];

    public static FilePickerFileType[] FilePickerIni { get; } =
    [
        new("INI files")
        {
            Patterns = ["*.ini"],
            AppleUniformTypeIdentifiers = ["public.plain-text"],
            MimeTypes = ["text/ini"]
        }
    ];

    public static IEnumerable<T> FindChildren<T>(this Visual parent)
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is T target)
                yield return target;

            foreach (var item in FindChildren<T>(child))
                yield return item;
        }
    }

    public static void ExtendDataGridShortcuts(this DataGrid dataGrid, Action<IList>? deleteSelectedRowsAction = null,
        Action<IEnumerable>? deleteAllRowsAction = null)
    {
        dataGrid.KeyUp += OnDataGridOnKeyUp;
        return;

        void OnDataGridOnKeyUp(object? sender, KeyEventArgs e)
        {
            if (sender is not DataGrid internalDataGrid) return;

            // The event bubbles from headers, group headers and the empty area below the rows as well.
            if (e.Source is DataGridCell ||
                (e.Source is Visual source && source.FindAncestorOfType<DataGridCell>(true) is not null)) return;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (e.Key == Key.Escape)
                {
                    internalDataGrid.SelectedIndex = -1;
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Multiply || (e.KeyModifiers.HasFlag(SystemAware.ControlOrMeta) && e.Key == Key.A))
                {
                    using var invertedList = internalDataGrid.ItemsSource
                        .AsValueEnumerable()
                        .Where(item => !internalDataGrid.SelectedItems.Contains(item))
                        .ToArrayPool();

                    internalDataGrid.SelectedItems.Clear();
                    foreach (var host in invertedList.Span)
                    {
                        internalDataGrid.SelectedItems.Add(host);
                    }

                    e.Handled = true;
                    return;
                }

                if (deleteAllRowsAction is not null
                    && e.KeyModifiers.HasFlag(SystemAware.ControlOrMeta)
                    && e.Key == Key.Delete)
                {
                    deleteAllRowsAction.Invoke(internalDataGrid.ItemsSource);
                    e.Handled = true;
                    return;
                }

                if (deleteSelectedRowsAction is not null
                    && e.Key == Key.Delete)
                {
                    deleteSelectedRowsAction.Invoke(internalDataGrid.SelectedItems);
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}