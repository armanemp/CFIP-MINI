using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class CFIPMini : Indicator
    {
        // ---------- General ----------
        [Parameter("Display Mode", DefaultValue = DisplayMode.Balanced, Group = "General")]
        public DisplayMode Mode { get; set; }

        [Parameter("Historical Days", DefaultValue = 5, MinValue = 1, MaxValue = 20, Group = "Performance")]
        public int HistoricalDays { get; set; }

        [Parameter("Max Profile Bars", DefaultValue = 900, MinValue = 100, MaxValue = 5000, Group = "Performance")]
        public int MaxProfileBars { get; set; }

        [Parameter("Show Panel", DefaultValue = true, Group = "General")]
        public bool ShowPanel { get; set; }

        // ---------- Time levels ----------
        [Parameter("Previous Day", DefaultValue = true, Group = "Time Levels")]
        public bool ShowPreviousDay { get; set; }

        [Parameter("Previous Week", DefaultValue = true, Group = "Time Levels")]
        public bool ShowPreviousWeek { get; set; }

        [Parameter("Sessions", DefaultValue = true, Group = "Time Levels")]
        public bool ShowSessions { get; set; }

        [Parameter("Session Start Hour UTC", DefaultValue = 0, MinValue = 0, MaxValue = 23, Group = "Sessions")]
        public int SessionStartHourUtc { get; set; }

        [Parameter("London Start UTC", DefaultValue = 8, MinValue = 0, MaxValue = 23, Group = "Sessions")]
        public int LondonStartUtc { get; set; }

        [Parameter("London End UTC", DefaultValue = 13, MinValue = 0, MaxValue = 23, Group = "Sessions")]
        public int LondonEndUtc { get; set; }

        [Parameter("New York Start UTC", DefaultValue = 13, MinValue = 0, MaxValue = 23, Group = "Sessions")]
        public int NewYorkStartUtc { get; set; }

        [Parameter("New York End UTC", DefaultValue = 21, MinValue = 0, MaxValue = 23, Group = "Sessions")]
        public int NewYorkEndUtc { get; set; }

        // ---------- Profile ----------
        [Parameter("Volume Profile", DefaultValue = true, Group = "Profile")]
        public bool ShowProfile { get; set; }

        [Parameter("Value Area %", DefaultValue = 70, MinValue = 50, MaxValue = 90, Group = "Profile")]
        public double ValueAreaPercent { get; set; }

        [Parameter("Profile Rows", DefaultValue = 48, MinValue = 12, MaxValue = 160, Group = "Profile")]
        public int ProfileRows { get; set; }

        [Parameter("Show HVN/LVN", DefaultValue = true, Group = "Profile")]
        public bool ShowHvnLvn { get; set; }

        // ---------- Structure ----------
        [Parameter("Structure", DefaultValue = true, Group = "Structure")]
        public bool ShowStructure { get; set; }

        [Parameter("Swing Strength", DefaultValue = 3, MinValue = 2, MaxValue = 10, Group = "Structure")]
        public int SwingStrength { get; set; }

        [Parameter("Liquidity", DefaultValue = true, Group = "Liquidity")]
        public bool ShowLiquidity { get; set; }

        [Parameter("Equal Level Tolerance ATR", DefaultValue = 0.10, MinValue = 0.02, MaxValue = 0.50, Group = "Liquidity")]
        public double EqualLevelToleranceAtr { get; set; }

        // ---------- Reaction ----------
        [Parameter("Reaction Engine", DefaultValue = true, Group = "Reaction")]
        public bool ShowReaction { get; set; }

        [Parameter("Reaction Distance ATR", DefaultValue = 0.20, MinValue = 0.05, MaxValue = 0.75, Group = "Reaction")]
        public double ReactionDistanceAtr { get; set; }

        [Parameter("Show Targets", DefaultValue = true, Group = "Reaction")]
        public bool ShowTargets { get; set; }

        // ---------- Range ----------
        [Parameter("ATR Period", DefaultValue = 14, MinValue = 5, MaxValue = 100, Group = "Range")]
        public int AtrPeriod { get; set; }

        [Parameter("Show Range Extensions", DefaultValue = true, Group = "Range")]
        public bool ShowRangeExtensions { get; set; }

        private readonly Dictionary<string, double> _levels = new Dictionary<string, double>();
        private readonly HashSet<string> _activeObjects = new HashSet<string>();

        private Bars _daily;
        private Bars _weekly;
        private double _atr;
        private DateTime _lastDailyOpen;
        private DateTime _lastRenderedBar;

        private ProfileResult _profile;
        private List<SwingPoint> _swings = new List<SwingPoint>();
        private List<Level> _liquidity = new List<Level>();

        protected override void Initialize()
        {
            _daily = MarketData.GetBars(TimeFrame.Daily, Symbol.Name);
            _weekly = MarketData.GetBars(TimeFrame.Weekly, Symbol.Name);

            CalculateAtr();
            Rebuild();
        }

        public override void Calculate(int index)
        {
            if (Bars.Count == 0)
                return;

            var currentBarTime = Bars.OpenTimes[Bars.Count - 1];

            // Heavy work only once per new chart bar.
            if (_lastRenderedBar != currentBarTime)
            {
                _lastRenderedBar = currentBarTime;
                CalculateAtr();
                Rebuild();
            }

            // Reaction is deliberately cheap and may update on every tick.
            if (ShowReaction)
                UpdateReaction();
        }

        private void Rebuild()
        {
            _levels.Clear();
            _activeObjects.Clear();

            BuildTimeLevels();
            BuildSessions();
            BuildProfile();
            BuildStructure();
            BuildLiquidity();
            BuildRangeExtensions();
            BuildConfluenceAndRender();
            UpdatePanel();
        }

        private void BuildTimeLevels()
        {
            if (ShowPreviousDay && _daily.Count >= 3)
            {
                int i = _daily.Count - 2;
                double h = _daily.HighPrices[i];
                double l = _daily.LowPrices[i];
                double o = _daily.OpenPrices[i];
                double c = _daily.ClosePrices[i];

                AddLevel("PDH", h, 92);
                AddLevel("PDL", l, 92);
                AddLevel("PDO", o, 78);
                AddLevel("PDC", c, 78);
                AddLevel("PDM", (h + l) * 0.5, 62);
                AddLevel("PD25", l + (h - l) * 0.25, 48);
                AddLevel("PD75", l + (h - l) * 0.75, 48);
            }

            if (ShowPreviousWeek && _weekly.Count >= 3)
            {
                int i = _weekly.Count - 2;
                double h = _weekly.HighPrices[i];
                double l = _weekly.LowPrices[i];
                double o = _weekly.OpenPrices[i];
                double c = _weekly.ClosePrices[i];

                AddLevel("PWH", h, 90);
                AddLevel("PWL", l, 90);
                AddLevel("PWO", o, 72);
                AddLevel("PWC", c, 72);
                AddLevel("PWM", (h + l) * 0.5, 58);
                AddLevel("PW25", l + (h - l) * 0.25, 44);
                AddLevel("PW75", l + (h - l) * 0.75, 44);
            }

            if (_daily.Count > 0)
            {
                int i = _daily.Count - 1;
                AddLevel("DO", _daily.OpenPrices[i], 86);
                AddLevel("DH", _daily.HighPrices[i], 66);
                AddLevel("DL", _daily.LowPrices[i], 66);
            }
        }

        private void BuildSessions()
        {
            if (!ShowSessions || Bars.Count < 2)
                return;

            DateTime now = Bars.OpenTimes[Bars.Count - 1];
            DateTime day = now.Date;

            AddSession("ASIA", day.AddHours(SessionStartHourUtc), WrapHour(day, LondonStartUtc));
            AddSession("LONDON", day.AddHours(LondonStartUtc), WrapHour(day, LondonEndUtc));
            AddSession("NEWYORK", day.AddHours(NewYorkStartUtc), WrapHour(day, NewYorkEndUtc));

            // Initial Balance: first London hour.
            var ibStart = day.AddHours(LondonStartUtc);
            var ibEnd = ibStart.AddHours(1);
            AddSession("IB", ibStart, ibEnd);
        }

        private DateTime WrapHour(DateTime day, int hour)
        {
            return day.AddHours(hour);
        }

        private void AddSession(string key, DateTime start, DateTime end)
        {
            double hi = double.MinValue;
            double lo = double.MaxValue;
            bool found = false;
            int startIndex = Bars.OpenTimes.GetIndexByTime(start);
            int endIndex = Bars.OpenTimes.GetIndexByTime(end);

            if (startIndex < 0)
                startIndex = 0;
            if (endIndex < 0 || endIndex >= Bars.Count)
                endIndex = Bars.Count - 1;

            int from = Math.Max(0, Math.Min(startIndex, endIndex));
            int to = Math.Min(Bars.Count - 1, Math.Max(startIndex, endIndex));

            for (int i = from; i <= to; i++)
            {
                DateTime t = Bars.OpenTimes[i];
                if (t < start || t >= end)
                    continue;

                hi = Math.Max(hi, Bars.HighPrices[i]);
                lo = Math.Min(lo, Bars.LowPrices[i]);
                found = true;
            }

            if (!found)
                return;

            int score = key == "IB" ? 82 : 70;
            AddLevel(key + "_H", hi, score);
            AddLevel(key + "_L", lo, score);
            AddLevel(key + "_M", (hi + lo) * 0.5, score - 18);
        }

        private void BuildProfile()
        {
            _profile = default(ProfileResult);
            if (!ShowProfile || Bars.Count < 20)
                return;

            int end = Bars.Count - 1;
            DateTime startTime = Bars.OpenTimes[end].Date;
            int start = Bars.OpenTimes.GetIndexByTime(startTime);
            if (start < 0)
                start = Math.Max(0, end - MaxProfileBars);

            start = Math.Max(start, end - MaxProfileBars);
            int count = end - start + 1;
            if (count < 10)
                return;

            double low = double.MaxValue;
            double high = double.MinValue;
            for (int i = start; i <= end; i++)
            {
                low = Math.Min(low, Bars.LowPrices[i]);
                high = Math.Max(high, Bars.HighPrices[i]);
            }

            if (high <= low)
                return;

            double step = (high - low) / ProfileRows;
            if (step <= Symbol.TickSize)
                step = Symbol.TickSize;

            var volume = new double[ProfileRows];

            for (int i = start; i <= end; i++)
            {
                double barLow = Bars.LowPrices[i];
                double barHigh = Bars.HighPrices[i];
                double v = Math.Max(1, Bars.TickVolumes[i]);
                int a = Clamp((int)Math.Floor((barLow - low) / step), 0, ProfileRows - 1);
                int b = Clamp((int)Math.Floor((barHigh - low) / step), 0, ProfileRows - 1);
                int rows = Math.Max(1, b - a + 1);
                double share = v / rows;

                for (int r = a; r <= b; r++)
                    volume[r] += share;
            }

            int poc = 0;
            for (int r = 1; r < ProfileRows; r++)
                if (volume[r] > volume[poc])
                    poc = r;

            double total = volume.Sum();
            double target = total * (ValueAreaPercent / 100.0);
            int left = poc;
            int right = poc;
            double covered = volume[poc];

            while (covered < target && (left > 0 || right < ProfileRows - 1))
            {
                double nextLeft = left > 0 ? volume[left - 1] : -1;
                double nextRight = right < ProfileRows - 1 ? volume[right + 1] : -1;

                if (nextRight >= nextLeft && right < ProfileRows - 1)
                {
                    right++;
                    covered += volume[right];
                }
                else if (left > 0)
                {
                    left--;
                    covered += volume[left];
                }
                else
                    break;
            }

            double pocPrice = low + (poc + 0.5) * step;
            double val = low + left * step;
            double vah = low + (right + 1) * step;

            _profile = new ProfileResult
            {
                Poc = pocPrice,
                Val = val,
                Vah = vah,
                Step = step,
                Volume = volume
            };

            AddLevel("POC", pocPrice, 88);
            AddLevel("VAL", val, 80);
            AddLevel("VAH", vah, 80);

            if (ShowHvnLvn)
            {
                int hvn = FindLocalExtreme(volume, true);
                int lvn = FindLocalExtreme(volume, false);
                if (hvn >= 0)
                    AddLevel("HVN", low + (hvn + 0.5) * step, 72);
                if (lvn >= 0)
                    AddLevel("LVN", low + (lvn + 0.5) * step, 68);
            }
        }

        private int FindLocalExtreme(double[] v, bool high)
        {
            if (v.Length < 3)
                return -1;

            int best = -1;
            double bestValue = high ? double.MinValue : double.MaxValue;

            for (int i = 1; i < v.Length - 1; i++)
            {
                bool local = high
                    ? v[i] >= v[i - 1] && v[i] >= v[i + 1]
                    : v[i] <= v[i - 1] && v[i] <= v[i + 1];

                if (!local)
                    continue;

                if ((high && v[i] > bestValue) || (!high && v[i] < bestValue))
                {
                    bestValue = v[i];
                    best = i;
                }
            }

            return best;
        }

        private void BuildStructure()
        {
            _swings = new List<SwingPoint>();
            if (!ShowStructure || Bars.Count < SwingStrength * 2 + 5)
                return;

            int last = Bars.Count - 1 - SwingStrength;
            int first = Math.Max(SwingStrength, last - Math.Max(200, HistoricalDays * 150));

            for (int i = first; i <= last; i++)
            {
                bool high = true;
                bool low = true;

                for (int j = 1; j <= SwingStrength; j++)
                {
                    if (Bars.HighPrices[i] <= Bars.HighPrices[i - j] || Bars.HighPrices[i] < Bars.HighPrices[i + j])
                        high = false;
                    if (Bars.LowPrices[i] >= Bars.LowPrices[i - j] || Bars.LowPrices[i] > Bars.LowPrices[i + j])
                        low = false;
                }

                if (high)
                    _swings.Add(new SwingPoint(i, Bars.HighPrices[i], true));
                if (low)
                    _swings.Add(new SwingPoint(i, Bars.LowPrices[i], false));
            }

            if (_swings.Count == 0)
                return;

            foreach (var s in _swings.OrderByDescending(x => x.Index).Take(8))
                AddLevel((s.IsHigh ? "SH_" : "SL_") + s.Index, s.Price, 64);
        }

        private void BuildLiquidity()
        {
            _liquidity = new List<Level>();
            if (!ShowLiquidity || _swings.Count < 2)
                return;

            double tolerance = Math.Max(Symbol.TickSize * 2, _atr * EqualLevelToleranceAtr);

            var highs = _swings.Where(x => x.IsHigh).OrderByDescending(x => x.Index).ToList();
            var lows = _swings.Where(x => !x.IsHigh).OrderByDescending(x => x.Index).ToList();

            FindEqual(highs, tolerance, true);
            FindEqual(lows, tolerance, false);
        }

        private void FindEqual(List<SwingPoint> points, double tolerance, bool high)
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (Math.Abs(points[i].Price - points[j].Price) > tolerance)
                        continue;

                    double price = (points[i].Price + points[j].Price) * 0.5;
                    var level = new Level("EQ_" + (high ? "H" : "L") + "_" + points[i].Index, price, 84);
                    _liquidity.Add(level);
                    AddLevel(level.Name, level.Price, level.Score);
                    break;
                }
            }
        }

        private void BuildRangeExtensions()
        {
            if (!ShowRangeExtensions || _daily.Count < 3)
                return;

            int i = _daily.Count - 2;
            double range = _daily.HighPrices[i] - _daily.LowPrices[i];
            if (range <= 0)
                return;

            AddLevel("PDH_EXT_1", _daily.HighPrices[i] + range * 0.5, 42);
            AddLevel("PDL_EXT_1", _daily.LowPrices[i] - range * 0.5, 42);
            AddLevel("PDH_EXT_2", _daily.HighPrices[i] + range, 34);
            AddLevel("PDL_EXT_2", _daily.LowPrices[i] - range, 34);

            AddLevel("PD_PREMIUM", _daily.LowPrices[i] + range * 0.5, 54);
            AddLevel("PD_DISCOUNT", _daily.LowPrices[i] + range * 0.5, 54);
        }

        private void BuildConfluenceAndRender()
        {
            var groups = _levels
                .Select(x => new Level(x.Key, x.Value, ScoreForName(x.Key)))
                .OrderByDescending(x => x.Score)
                .ToList();

            var zones = new List<Zone>();
            double tolerance = Math.Max(Symbol.TickSize * 3, _atr * 0.12);

            foreach (var level in groups)
            {
                Zone zone = null;
                foreach (var z in zones)
                {
                    if (Math.Abs(z.Price - level.Price) <= tolerance)
                    {
                        zone = z;
                        break;
                    }
                }

                if (zone == null)
                {
                    zone = new Zone(level.Price);
                    zones.Add(zone);
                }

                zone.Levels.Add(level);
                zone.Price = zone.Levels.Average(x => x.Price);
            }

            zones = zones
                .OrderByDescending(z => z.Strength)
                .Take(Mode == DisplayMode.Clean ? 10 : Mode == DisplayMode.Balanced ? 18 : 35)
                .ToList();

            foreach (var z in zones)
            {
                string name = "CFIPM_ZONE_" + Math.Round(z.Price / Symbol.TickSize);
                DrawLine(name, z.Price, z.Strength >= 150 ? Color.Gold : Color.Gray, z.Strength >= 150 ? 2 : 1);
                DrawLabel(name + "_TXT", z.Label, z.Price, z.Strength >= 150 ? Color.Gold : Color.Gray);
            }
        }

        private int ScoreForName(string name)
        {
            if (name.StartsWith("PDH") || name.StartsWith("PDL") || name.StartsWith("PWH") || name.StartsWith("PWL"))
                return 92;
            if (name == "POC" || name == "VAH" || name == "VAL")
                return 84;
            if (name.StartsWith("EQ_"))
                return 84;
            if (name.StartsWith("DO"))
                return 78;
            if (name.StartsWith("IB_"))
                return 78;
            return 55;
        }

        private void UpdateReaction()
        {
            if (Bars.Count == 0 || _levels.Count == 0)
                return;

            double price = Symbol.Bid;
            double distance = Math.Max(Symbol.TickSize * 3, _atr * ReactionDistanceAtr);

            Level nearest = null;
            double nearestDistance = double.MaxValue;

            foreach (var item in _levels)
            {
                double d = Math.Abs(price - item.Value);
                if (d < nearestDistance && d <= distance)
                {
                    nearestDistance = d;
                    nearest = new Level(item.Key, item.Value, ScoreForName(item.Key));
                }
            }

            if (nearest == null)
                return;

            int last = Bars.Count - 1;
            double high = Bars.HighPrices[last];
            double low = Bars.LowPrices[last];
            double open = Bars.OpenPrices[last];
            double close = Bars.ClosePrices[last];

            bool above = price > nearest.Price;
            bool pierced = above ? low <= nearest.Price : high >= nearest.Price;
            bool rejection = pierced && ((above && close > nearest.Price && close > open) ||
                                         (!above && close < nearest.Price && close < open));
            bool acceptance = above
                ? close > nearest.Price && low > nearest.Price
                : close < nearest.Price && high < nearest.Price;

            string state = rejection ? "REJECTION" : acceptance ? "ACCEPTANCE" : pierced ? "TEST" : "NEAR";
            int score = nearest.Score + (rejection ? 15 : acceptance ? 12 : 0);

            string panel = "CFIP MINI | " + Symbol.Name + "\n" +
                           "Nearest: " + nearest.Name + "  " + FormatPrice(nearest.Price) + "\n" +
                           "State: " + state + "  Strength: " + score + "\n" +
                           "Bias: " + (rejection ? (above ? "DOWN pressure" : "UP pressure") :
                                        acceptance ? (above ? "UP continuation" : "DOWN continuation") :
                                        "WAIT / CONFIRM");

            if (ShowPanel)
                Chart.DrawStaticText("CFIPM_PANEL", panel, VerticalAlignment.Top, HorizontalAlignment.Right, Color.White);
        }

        private void UpdatePanel()
        {
            if (!ShowPanel)
                return;

            string profile = _profile.Poc > 0
                ? "POC " + FormatPrice(_profile.Poc) + " | VA " + FormatPrice(_profile.Val) + " - " + FormatPrice(_profile.Vah)
                : "Profile: n/a";

            string text = "CFIP MINI\n" +
                          Symbol.Name + " | " + TimeFrame.ShortName + "\n" +
                          profile + "\n" +
                          "Levels: " + _levels.Count + " | Swings: " + _swings.Count + "\n" +
                          "Tick-volume profile = estimate";

            Chart.DrawStaticText("CFIPM_PANEL", text, VerticalAlignment.Top, HorizontalAlignment.Right, Color.White);
        }

        private void CalculateAtr()
        {
            if (Bars.Count < AtrPeriod + 2)
            {
                _atr = Symbol.PipSize * 20;
                return;
            }

            double sum = 0;
            int n = Math.Min(AtrPeriod, Bars.Count - 1);

            for (int i = Bars.Count - n; i < Bars.Count; i++)
            {
                double prevClose = Bars.ClosePrices[i - 1];
                double tr = Math.Max(
                    Bars.HighPrices[i] - Bars.LowPrices[i],
                    Math.Max(Math.Abs(Bars.HighPrices[i] - prevClose),
                             Math.Abs(Bars.LowPrices[i] - prevClose)));
                sum += tr;
            }

            _atr = Math.Max(Symbol.TickSize, sum / n);
        }

        private void AddLevel(string name, double price, int score)
        {
            if (double.IsNaN(price) || double.IsInfinity(price))
                return;

            _levels[name] = price;
        }

        private void DrawLine(string name, double price, Color color, int thickness)
        {
            var line = Chart.DrawHorizontalLine(name, price, color, thickness, LineStyle.Solid);
            line.IsInteractive = false;
            _activeObjects.Add(name);
        }

        private void DrawLabel(string name, string text, double price, Color color)
        {
            DateTime time = Bars.OpenTimes[Math.Max(0, Bars.Count - 1)];
            Chart.DrawText(name, text, time, price, color);
            _activeObjects.Add(name);
        }

        private string FormatPrice(double price)
        {
            return price.ToString("F" + Symbol.Digits);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        public enum DisplayMode
        {
            Clean,
            Balanced,
            Full
        }

        private struct ProfileResult
        {
            public double Poc;
            public double Val;
            public double Vah;
            public double Step;
            public double[] Volume;
        }

        private sealed class SwingPoint
        {
            public SwingPoint(int index, double price, bool isHigh)
            {
                Index = index;
                Price = price;
                IsHigh = isHigh;
            }

            public int Index;
            public double Price;
            public bool IsHigh;
        }

        private sealed class Level
        {
            public Level(string name, double price, int score)
            {
                Name = name;
                Price = price;
                Score = score;
            }

            public string Name;
            public double Price;
            public int Score;
        }

        private sealed class Zone
        {
            public Zone(double price)
            {
                Price = price;
                Levels = new List<Level>();
            }

            public double Price;
            public List<Level> Levels;

            public int Strength
            {
                get { return Levels.Sum(x => x.Score); }
            }

            public string Label
            {
                get
                {
                    var names = Levels
                        .OrderByDescending(x => x.Score)
                        .Take(3)
                        .Select(x => x.Name);

                    return string.Join(" + ", names) + " [" + Strength + "]";
                }
            }
        }
    }
}
