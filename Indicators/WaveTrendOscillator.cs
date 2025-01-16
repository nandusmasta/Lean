/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Diagnostics;
using System.Linq;

public class WaveTrendOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
{
    private readonly int _channelPeriod;
    private readonly int _averagePeriod;
    private readonly int _smoothPeriod;
    private readonly decimal _channelMultiplier;
    private readonly RollingWindow<decimal> _typicalPrices;

    private readonly ExponentialMovingAverage _exponentialSmoothedAverage;
    private readonly ExponentialMovingAverage _absoluteDeviation;
    private readonly ExponentialMovingAverage _channelIndex;
    private readonly SimpleMovingAverage _smoothedWaveTrend;

    /// <summary>
    /// Gets the WaveTrend indicator.
    /// </summary>
    public IndicatorBase<IndicatorDataPoint> WaveTrend { get; }
    /// <summary>
    /// Gets the smoothed WaveTrend indicator.
    /// </summary>
    public IndicatorBase<IndicatorDataPoint> WaveTrendSmooth { get; }

    public override bool IsReady => Samples > WarmUpPeriod && _exponentialSmoothedAverage.IsReady && _absoluteDeviation.IsReady &&
    _channelIndex.IsReady && _smoothedWaveTrend.IsReady && WaveTrend.IsReady && WaveTrendSmooth.IsReady;

    public int WarmUpPeriod => 150;

    public WaveTrendOscillator(string name, int channelPeriod = 10, int averagePeriod = 21,
        int smoothPeriod = 4, decimal channelMultiplier = 0.015m)
        : base(name)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        _channelPeriod = channelPeriod;
        _averagePeriod = averagePeriod;
        _smoothPeriod = smoothPeriod;
        _channelMultiplier = channelMultiplier;

        // Initialize indicators
        _exponentialSmoothedAverage = new ExponentialMovingAverage(channelPeriod);
        _absoluteDeviation = new ExponentialMovingAverage(channelPeriod);
        _channelIndex = new ExponentialMovingAverage(averagePeriod);
        _smoothedWaveTrend = new SimpleMovingAverage(smoothPeriod);

        WaveTrend = new Identity(name + "_TCI");
        WaveTrendSmooth = new Identity(name + "_WT2");

        _typicalPrices = new RollingWindow<decimal>(channelPeriod + averagePeriod + smoothPeriod);
    }

    public WaveTrendOscillator(int channelPeriod = 10, int averagePeriod = 21, int smoothPeriod = 4,
        decimal channelMultiplier = 0.015m)
        : this($"WTO({channelPeriod},{averagePeriod},{smoothPeriod})", channelPeriod,
              averagePeriod, smoothPeriod, channelMultiplier)
    {
    }

    protected override decimal ComputeNextValue(IBaseDataBar input)
    {
        decimal typicalPrice = (input.High + input.Low + input.Close) / 3m;
        _typicalPrices.Add(typicalPrice);

        // Only update ESA after collecting enough data for SMA
        if (_typicalPrices.Count < _channelPeriod)
        {
            return 0m;
        }
        else if (_typicalPrices.Count == _channelPeriod)
        {
            // Initialize ESA with SMA
            decimal sma = _typicalPrices.Average();
            _exponentialSmoothedAverage.Reset();
            _exponentialSmoothedAverage.Update(input.Time, sma);
        }

        _exponentialSmoothedAverage.Update(input.Time, typicalPrice);

        if (!_exponentialSmoothedAverage.IsReady) 
        { 
            return 0m; 
        }

        decimal absDiff = Math.Abs(typicalPrice - _exponentialSmoothedAverage.Current.Value);
        _absoluteDeviation.Update(input.Time, absDiff);

        if (!_absoluteDeviation.IsReady) 
        { 
            return 0m; 
        }

        decimal denominator = _channelMultiplier * _absoluteDeviation.Current.Value;
        if (denominator == 0m) 
        {
            Debug.WriteLine("Divide by zero avoided");
            return 0m; 
        }

        decimal ci = (typicalPrice - _exponentialSmoothedAverage.Current.Value) / denominator;

        _channelIndex.Update(input.Time, ci);

        if (!_channelIndex.IsReady)
        {
            return 0m;
        }
        WaveTrend.Update(input.Time, _channelIndex.Current.Value);
        _smoothedWaveTrend.Update(input.Time, _channelIndex.Current.Value);

        if (_smoothedWaveTrend.IsReady)
        {
            WaveTrendSmooth.Update(input.Time, _smoothedWaveTrend.Current.Value);
        }

        Debug.WriteLine("**********************************************************");
        Debug.WriteLine($"Open:{input.Time}, Open:{input.Open}, High:{input.High}, " +
            $"Low:{input.Low}, Close:{input.Close}");
        Debug.WriteLine($"ESA: {_exponentialSmoothedAverage.Current.Value}");
        Debug.WriteLine($"D: {_absoluteDeviation.Current.Value}");
        Debug.WriteLine($"CI={ci}");
        Debug.WriteLine($"TCI: {_channelIndex.Current.Value}");
        Debug.WriteLine($"WT2: {_smoothedWaveTrend.Current.Value}");
        Debug.WriteLine("**********************************************************");

        return _channelIndex.Current.Value;
    }

    public override void Reset()
    {
        _typicalPrices.Reset();
        _exponentialSmoothedAverage.Reset();
        _absoluteDeviation.Reset();
        _channelIndex.Reset();
        _smoothedWaveTrend.Reset();
        WaveTrend.Reset();
        WaveTrendSmooth.Reset();
        base.Reset();
    }
}
