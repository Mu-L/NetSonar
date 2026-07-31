using System;
using System.Collections.Specialized;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore;
using NetSonar.Avalonia.Network;
using ObservableCollections;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using NetSonar.Avalonia.Extensions;
using LiveChartsCore.Measure;
using LiveChartsCore.Painting;
using SukiUI.Models;
using ZLinq;

namespace NetSonar.Avalonia.ViewModels.Fragments;

public partial class PingableServiceGraphFragmentModel : ViewModelBase, IDisposable
{
    private bool _isDisposed;
    private PingableService[] _services = [];
    public PingableService[] Services
    {
        get => _services;
        set
        {
            foreach (var service in _services)
            {
                service.PingCompleted -= ServiceOnPingCompleted;
                service.Pings.CollectionChanged -= ServicePingsOnCollectionChanged;
            }

            if (value.Length <= 1)
            {
                _services = value;
            }
            else
            {
                _services = value
                    .AsValueEnumerable()
                    .OrderByDescending(service => service.AverageTime)
                    .ThenByDescending(service => service.LastTime)
                    .ToArray();
            }

            foreach (var service in _services)
            {
                service.PingCompleted += ServiceOnPingCompleted;
                service.Pings.CollectionChanged += ServicePingsOnCollectionChanged;
            }

            Rebuild();

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasService));
            OnPropertyChanged(nameof(HasSingleService));
            OnPropertyChanged(nameof(HasMultiServices));
        }
    }

    public bool HasService => Services.Length > 0;
    public bool HasSingleService => Services.Length == 1;
    public bool HasMultiServices => Services.Length > 1;

    private readonly ISeries[] _singleGraphSeries;
    private readonly ICartesianAxis[] _singleGraphXAxes;
    private readonly ICartesianAxis[] _singleGraphYAxes;

    private readonly ISeries[] _multiGraphSeries;
    private readonly ICartesianAxis[] _multiGraphXAxes;
    private readonly ICartesianAxis[] _multiGraphYAxes;


    [ObservableProperty]
    public partial ISeries[] GraphSeries { get; private set; } = [];

    [ObservableProperty]
    public partial ICartesianAxis[] GraphXAxes { get; private set; } = [new Axis()];

    [ObservableProperty]
    public partial ICartesianAxis[] GraphYAxes { get; private set; } = [new Axis()];


    private readonly ObservableList<double> _repliesMinimumTimeValues = [];
    public NotifyCollectionChangedSynchronizedViewList<double> RepliesMinimumTimeValuesCollection { get; }

    private readonly ObservableList<double> _repliesTimeValues = [];
    public NotifyCollectionChangedSynchronizedViewList<double> RepliesTimeValuesCollection { get; }

    private readonly ObservableList<double> _repliesAvgTimeValues = [];
    public NotifyCollectionChangedSynchronizedViewList<double> RepliesAvgTimeValuesCollection { get; }

    private readonly ObservableList<double> _repliesMaximumTimeValues = [];
    public NotifyCollectionChangedSynchronizedViewList<double> RepliesMaximumTimeValuesCollection { get; }


    private readonly ObservableList<string> _repliesXAxes  = [];
    public NotifyCollectionChangedSynchronizedViewList<string> RepliesXAxesCollection { get; }

    private readonly ObservableList<string> _repliesYAxes = [];
    public NotifyCollectionChangedSynchronizedViewList<string> RepliesYAxesCollection { get; }

    [ObservableProperty]
    public partial Func<float, float>? EasingFunction { get; private set; } = LiveCharts.DefaultSettings.EasingFunction;

    [ObservableProperty] public partial Paint ThemePaint { get; set; } = new SolidColorPaint(new SKColor(255, 255, 255));



    public PingableServiceGraphFragmentModel()
    {
        RepliesMinimumTimeValuesCollection = _repliesMinimumTimeValues.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);
        RepliesTimeValuesCollection = _repliesTimeValues.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);
        RepliesAvgTimeValuesCollection = _repliesAvgTimeValues.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);
        RepliesMaximumTimeValuesCollection = _repliesMaximumTimeValues.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);

        RepliesXAxesCollection = _repliesXAxes.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);
        RepliesYAxesCollection = _repliesYAxes.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current);



        _singleGraphSeries =
        [
            new ColumnSeries<double>(RepliesTimeValuesCollection)
            {
                Name = "Ping",
                Mapping = MapFiniteValue,
                // Defines the distance between every bars in the series
                Padding = 0,
                YToolTipLabelFormatter = point => $"{point.Model}ms",
                //DataLabelsPaint = new SolidColorPaint(new SKColor(Brushes.White.Color.R, Brushes.White.Color.G, Brushes.White.Color.B)),
                // Defines the max width a bar can have
                Fill = new SolidColorPaint(new SKColor(
                    App.Theme.ActiveColorTheme!.Primary.R,
                    App.Theme.ActiveColorTheme.Primary.G,
                    App.Theme.ActiveColorTheme.Primary.B)),
                MaxBarWidth = double.MaxValue,
            },
            new LineSeries<double>(RepliesAvgTimeValuesCollection)
            {
                Name = "Average",
                Mapping = MapFiniteValue,
                //Fill = null,
                //Stroke = null,
                YToolTipLabelFormatter = point => $"{point.Model}ms",
                GeometryStroke = null,
                GeometryFill = null,
                //IsVisible = true,
                //GeometrySize = 20
            },
        ];

        _singleGraphXAxes = [
            new Axis
            {
                //Name = "Date time",
                Labels = RepliesXAxesCollection,
                LabelsRotation = 45,
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                NamePaint = new SolidColorPaint(new SKColor(255, 255, 255)),
            }
        ];

        _singleGraphYAxes = [
            new Axis
            {
                //Name = "Ping",
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                NamePaint = new SolidColorPaint(new SKColor(255, 255, 255)),
            }
        ];


        _multiGraphSeries =
        [
            new RowSeries<double>
            {
                Name = "Maximum",
                Values = RepliesMaximumTimeValuesCollection,
                Mapping = MapFiniteValue,
                IgnoresBarPosition = true,
                YToolTipLabelFormatter = point => $"{point.Model}ms",
                Fill = new SolidColorPaint(new SKColor(127, 127, 127, 255)),
                DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                DataLabelsTranslate = new(-1, 0),
            },

            new RowSeries<double>
            {
                Name = "Average",
                Values = RepliesAvgTimeValuesCollection,
                Mapping = MapFiniteValue,
                IgnoresBarPosition = true,
                YToolTipLabelFormatter = point => $"{point.Model}ms",
                Fill = new SolidColorPaint(new SKColor(Brushes.DarkOrange.Color.ToUInt32())),
                DataLabelsPaint = new SolidColorPaint(new SKColor(0, 0, 0)),
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                DataLabelsTranslate = new(-1, 0),
            },

            new RowSeries<double>
            {
                Name = "Current",
                Values = RepliesTimeValuesCollection,
                Mapping = MapFiniteValue,
                IgnoresBarPosition = true,
                MaxBarWidth = 25,
                YToolTipLabelFormatter = point => $"{point.Model}ms",
                Fill = new SolidColorPaint(new SKColor(Brushes.DarkCyan.Color.ToUInt32())),
                DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                DataLabelsTranslate = new(-1, 0),
            },

            new RowSeries<double>
            {
                Name = "Minimum",
                Values = RepliesMinimumTimeValuesCollection,
                Mapping = MapFiniteValue,
                IgnoresBarPosition = true,
                YToolTipLabelFormatter = point => $"{point.Model}ms",
                Fill = new SolidColorPaint(new SKColor(Brushes.DarkGreen.Color.ToUInt32())),
                DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                DataLabelsTranslate = new(-1, 0),
            },
        ];

        _multiGraphXAxes = [
            new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255))
               // SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220))
            }
        ];

        _multiGraphYAxes = [
            new Axis
            {
                Labels = RepliesYAxesCollection,
                Position = AxisPosition.End,
                LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                // SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220))
            }
        ];

        App.Theme.OnBaseThemeChanged += OnBaseThemeChanged;
        App.Theme.OnColorThemeChanged += OnColorThemeChanged;

        OnBaseThemeChanged(App.Theme.ActiveBaseTheme);
        OnColorThemeChanged(App.Theme.ActiveColorTheme);
    }

    public PingableServiceGraphFragmentModel(PingableService[] services) : this()
    {
        Services = services;
    }

    public void Rebuild()
    {
        if (HasSingleService)
        {
            GraphSeries = _singleGraphSeries;
            GraphXAxes = _singleGraphXAxes;
            GraphYAxes = _singleGraphYAxes;
            EasingFunction = null;

            _repliesTimeValues.Clear();
            _repliesAvgTimeValues.Clear();
            _repliesXAxes.Clear();

            if (!HasService) return;

            var service = Services[0];

            var maxReplies = AppSettings.PingServices.MaxRepliesGraphCache;
            var replies = service.Pings
                .AsValueEnumerable()
                .Take(maxReplies > 0 ? maxReplies : int.MaxValue)
                .ToArray();

            if (replies.Length == 0) return;

            Array.Reverse(replies);

            var timeValues = new double[replies.Length];
            var avgTimeValues = new double[replies.Length];
            var labels = new string[replies.Length];

            for (var i = 0; i < replies.Length; i++)
            {
                var reply = replies[i];
                timeValues[i] = reply.Time;
                labels[i] = reply.SentDateTime.ToLongTimeString();
            }

            FillHistoricalAverageValues(service, replies, avgTimeValues);

            _repliesTimeValues.AddRange(timeValues);
            _repliesAvgTimeValues.AddRange(avgTimeValues);
            _repliesXAxes.AddRange(labels);
        }
        else
        {
            GraphSeries = _multiGraphSeries;
            GraphXAxes = _multiGraphXAxes;
            GraphYAxes = _multiGraphYAxes;
            EasingFunction = LiveCharts.DefaultSettings.EasingFunction;

            _repliesMinimumTimeValues.Clear();
            _repliesTimeValues.Clear();
            _repliesAvgTimeValues.Clear();
            _repliesMaximumTimeValues.Clear();
            _repliesXAxes.Clear();
            _repliesYAxes.Clear();

            if (!HasService) return;

            foreach (var service in Services)
            {
                _repliesMinimumTimeValues.Add(service.MinimumTime);
                _repliesTimeValues.Add(service.LastTime);
                _repliesAvgTimeValues.Add(service.AverageTime);
                _repliesMaximumTimeValues.Add(service.MaximumTime);
                _repliesYAxes.Add(string.IsNullOrWhiteSpace(service.HostName) ? service.IpEndPointStr : service.HostName);
            }
        }
    }

    private void ServiceOnPingCompleted(object? sender, BasePingableCollectionObject<PingableServiceReply>.PingCompletedEventArgs e)
    {
        if (sender is not PingableService service) return;

        if (HasSingleService)
        {
            _repliesTimeValues.Add(e.Reply.Time);
            _repliesAvgTimeValues.Add(service.AverageTime);
            _repliesXAxes.Add(e.Reply.SentDateTime.ToLongTimeString());

            var maxReplies = AppSettings.PingServices.MaxRepliesGraphCache;
            if (maxReplies > 0)
            {
                _repliesTimeValues.RemoveExceedingAt(maxReplies, CollectionSide.Head);
                _repliesAvgTimeValues.RemoveExceedingAt(maxReplies, CollectionSide.Head);
                _repliesXAxes.RemoveExceedingAt(maxReplies, CollectionSide.Head);
            }
        }
        else
        {
            //var index = FindIndexForService(service);
            //if (index == -1 || _repliesMinimumTimeValues.Count <= index) return;

            //if (_repliesMinimumTimeValues[index] != service.MinimumTime) _repliesMinimumTimeValues[index] = service.MinimumTime;
            //if (_repliesTimeValues[index] != service.LastTime)_repliesTimeValues[index] = service.LastTime;
            //if (_repliesAvgTimeValues[index] != service.AverageTime) _repliesAvgTimeValues[index] = service.AverageTime;
            //if (_repliesMaximumTimeValues[index] != service.MaximumTime) _repliesMaximumTimeValues[index] = service.MaximumTime;

            Sort();
        }
    }

    private void ServicePingsOnCollectionChanged(in NotifyCollectionChangedEventArgs<PingableServiceReply> e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            Rebuild();
        }
    }

    private static Coordinate MapFiniteValue(double value, int index)
    {
        return double.IsFinite(value)
            ? new Coordinate(index, value)
            : Coordinate.Empty;
    }

    private static void FillHistoricalAverageValues(
        PingableService service,
        PingableServiceReply[] replies,
        double[] averageValues)
    {
        var totalTime = service.TotalTimeSum;
        var successCount = service.SucceedCount;

        for (var i = replies.Length - 1; i >= 0; i--)
        {
            averageValues[i] = successCount > 0
                ? Math.Round(totalTime / (double)successCount, 2, MidpointRounding.AwayFromZero)
                : double.PositiveInfinity;

            var reply = replies[i];
            if (!reply.IsSucceeded) continue;

            successCount--;
            if (double.IsFinite(reply.Time))
            {
                totalTime -= Convert.ToUInt64(reply.Time);
            }
        }
    }

    public int FindIndexForService(PingableService service)
    {
        var index = -1;
        for (var i = 0; i < Services.Length; i++)
        {
            if (Services[i] == service) return i;
        }

        return index;
    }

    public void Sort()
    {
        if (!HasMultiServices) return;

        _services = _services
            .AsValueEnumerable()
            .OrderByDescending(service => service.AverageTime)
            .ThenByDescending(service => service.LastTime)
            .ToArray();

        for (var i = 0; i < Services.Length; i++)
        {
            var service = Services[i];
            if (_repliesMinimumTimeValues[i] != service.MinimumTime) _repliesMinimumTimeValues[i] = service.MinimumTime;
            if (_repliesTimeValues[i] != service.LastTime) _repliesTimeValues[i] = service.LastTime;
            if (_repliesAvgTimeValues[i] != service.AverageTime) _repliesAvgTimeValues[i] = service.AverageTime;
            if (_repliesMaximumTimeValues[i] != service.MaximumTime) _repliesMaximumTimeValues[i] = service.MaximumTime;


            var label = string.IsNullOrWhiteSpace(service.HostName) ? service.IpEndPointStr : service.HostName;
            if (RepliesYAxesCollection[i] != label) _repliesYAxes[i] = label;
        }
    }


    private void OnBaseThemeChanged(ThemeVariant theme)
    {
        var color = App.SukiTextResource;
        var paint = new SolidColorPaint(new SKColor(color.R, color.G, color.B, color.A));

        foreach (var axis in _singleGraphXAxes)
        {
            axis.LabelsPaint = paint;
            axis.NamePaint = paint;
        }

        foreach (var axis in _singleGraphYAxes)
        {
            axis.LabelsPaint = paint;
            axis.NamePaint = paint;
        }

        foreach (var axis in _multiGraphXAxes)
        {
            axis.LabelsPaint = paint;
            axis.NamePaint = paint;
        }

        foreach (var axis in _multiGraphYAxes)
        {
            axis.LabelsPaint = paint;
            axis.NamePaint = paint;
        }
    }

    private void OnColorThemeChanged(SukiColorTheme theme)
    {
        if (_singleGraphSeries.Length == 0) return;
        if (_singleGraphSeries[0] is ColumnSeries<double> column)
        {
            column.Fill = new SolidColorPaint(new SKColor(
                theme.Primary.R,
                theme.Primary.G,
                theme.Primary.B));
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        App.Theme.OnBaseThemeChanged -= OnBaseThemeChanged;
        App.Theme.OnColorThemeChanged -= OnColorThemeChanged;

        Services = [];
        RepliesMinimumTimeValuesCollection.Dispose();
        RepliesTimeValuesCollection.Dispose();
        RepliesAvgTimeValuesCollection.Dispose();
        RepliesMaximumTimeValuesCollection.Dispose();
        RepliesXAxesCollection.Dispose();
        RepliesYAxesCollection.Dispose();
    }
}
