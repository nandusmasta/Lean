using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class WaveTrendOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new WaveTrendOscillator(10, 21, 4);
        }

        protected override string TestFileName => "spy_wto.csv";

        protected override string TestColumnName => "TCI";

        [Test]
        public void ComparesWithExternalDataWT2()
        {
            var wto = new WaveTrendOscillator(10, 21, 4);
            TestHelper.TestIndicator(wto.WaveTrend, "TCI", 0.001);
            TestHelper.TestIndicator(wto.WaveTrendSmooth, "WT2", 0.001);
        }

        [Test]
        public void ResetsProperly()
        {
            WaveTrendOscillator waveTrend = (WaveTrendOscillator) CreateIndicator();
            foreach (var data in TestHelper.GetTradeBarStream(TestFileName, false))
            {
                waveTrend.Update(data);
            }
            Assert.IsTrue(waveTrend.IsReady);
            Assert.IsTrue(waveTrend.WaveTrendSmooth.IsReady);

            waveTrend.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(waveTrend);
            TestHelper.AssertIndicatorIsInDefaultState(waveTrend.WaveTrendSmooth);
        }

        [Test]
        public void WarmsUpProperly()
        {
            WaveTrendOscillator indicator = (WaveTrendOscillator)CreateIndicator();
            var period = indicator.WarmUpPeriod;
            var samples = 0;

            foreach (var data in TestHelper.GetTradeBarStream(TestFileName, false))
            {
                indicator.Update(data);
                samples++;
                if (samples < period)
                {
                    Assert.IsFalse(indicator.IsReady);
                }
            }
            Assert.IsTrue(indicator.IsReady);
        }
    }
}
