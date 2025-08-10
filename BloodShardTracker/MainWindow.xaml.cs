using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BloodShardTracker.Models;

namespace BloodShardTracker
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<ShardDrop> _drops = new ObservableCollection<ShardDrop>();
        private readonly string saveFile = "bloodshards.json";

        public MainWindow()
        {
            InitializeComponent();

            grid.ItemsSource = _drops;
            DateInput.SelectedDate = DateTime.Now;
            HourInput.Text = DateTime.Now.Hour.ToString("00");
            MinuteInput.Text = DateTime.Now.Minute.ToString("00");

            UpdateStats();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (DateInput.SelectedDate == null)
            {
                MessageBox.Show("Choose a date.");
                return;
            }
            if (!int.TryParse(HourInput.Text, out int hh) || !int.TryParse(MinuteInput.Text, out int mm))
            {
                MessageBox.Show("Enter time as HH and mm.");
                return;
            }
            if (!long.TryParse(new string(PriceInput.Text.Where(char.IsDigit).ToArray()), out var price))
            {
                MessageBox.Show("Enter a price in gp.");
                return;
            }

            _drops.Add(new ShardDrop
            {
                When = DateInput.SelectedDate.Value.Date + new TimeSpan(hh, mm, 0),
                PriceGp = price
            });

            UpdateStats();
            PriceInput.Clear();
        }

        private void Parse_Click(object sender, RoutedEventArgs e)
        {
            string text = PasteInput.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            var vyreLine = new Regex(@"^\s*Vyrewatch\s+Sentinel:\s*$", RegexOptions.IgnoreCase);
            var pricePattern = new Regex(@"\bBlood\s*shard\b.*?\(([^)]+)\)", RegexOptions.IgnoreCase);

            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            int added = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!vyreLine.IsMatch(lines[i])) continue;

                long? gp = null;

                // Look ahead a few lines for the price line; skip noise like "Image"
                for (int j = i + 1; j <= Math.Min(i + 6, lines.Length - 1) && gp == null; j++)
                {
                    var look = lines[j].Trim();
                    if (look.Equals("Image", StringComparison.OrdinalIgnoreCase)) continue;

                    var m = pricePattern.Match(look);
                    if (m.Success)
                    {
                        var inside = m.Groups[1].Value.Trim(); // e.g., 9.00M or 9,700,000

                        // Comma format: 9,700,000
                        if (Regex.IsMatch(inside, @"^[0-9]{1,3}(?:,[0-9]{3})+(?:\.[0-9]+)?$"))
                        {
                            var digits = new string(inside.Where(char.IsDigit).ToArray());
                            if (long.TryParse(digits, out var vComma)) gp = vComma;
                        }
                        else
                        {
                            // Suffix: 9.00M, 900k, 13M
                            var suf = Regex.Match(inside, @"^([0-9]+(?:\.[0-9]+)?)\s*([kKmMbB])$");
                            if (suf.Success)
                            {
                                var num = double.Parse(suf.Groups[1].Value, CultureInfo.InvariantCulture);
                                var s = suf.Groups[2].Value.ToLowerInvariant();
                                double mult = s switch
                                {
                                    "k" => 1_000d,
                                    "m" => 1_000_000d,
                                    "b" => 1_000_000_000d,
                                    _ => 1d
                                };
                                gp = (long)Math.Round(num * mult, MidpointRounding.AwayFromZero);
                            }
                            else
                            {
                                // Raw digits fallback
                                var digits = new string(inside.Where(char.IsDigit).ToArray());
                                if (digits.Length > 0 && long.TryParse(digits, out var vRaw)) gp = vRaw;
                            }
                        }
                    }
                }

                if (gp != null)
                {
                    _drops.Add(new ShardDrop { When = DateTime.Now, PriceGp = gp.Value });
                    added++;
                }
            }

            UpdateStats();
            MessageBox.Show($"Imported {added} shard(s).");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var ans = MessageBox.Show(
                "Save now? This will overwrite your current bloodshards.json.",
                "Confirm Save",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (ans != MessageBoxResult.Yes) return;

            try
            {
                // If you're using a fixed path (e.g., Documents), make sure the folder exists:
                var dir = Path.GetDirectoryName(saveFile);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_drops, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveFile, json);

                MessageBox.Show("Saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }



        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(saveFile)) return;

            try
            {
                var text = File.ReadAllText(saveFile);
                var data = JsonSerializer.Deserialize<ObservableCollection<ShardDrop>>(text);
                if (data != null)
                {
                    _drops = data;
                    grid.ItemsSource = _drops;
                    UpdateStats();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load: " + ex.Message);
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Delete all drops?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            _drops.Clear();
            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalCount.Text = _drops.Count.ToString();
            long totalGp = _drops.Sum(d => d.PriceGp);
            TotalGp.Text = totalGp.ToString("N0") + " gp";
            AvgGp.Text = (_drops.Count > 0 ? (totalGp / _drops.Count).ToString("N0") : "0") + " gp";
        }
    }
}
