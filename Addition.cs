using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ooui;
using Xamarin.Forms;

namespace MathTree
{
    public class Addition
    {
        private Dictionary<Entry, (int pin, bool correct)> _results = new Dictionary<Entry, (int pin, bool correct)>();
        private Gpio _gpio = new Gpio();
        private bool _blink;
        Ooui.Element Create()
        {
            var counter = 4;
            var random = new Random();
            var laoyout = new StackLayout
            {
                Padding = new Thickness(50),
                HorizontalOptions = LayoutOptions.CenterAndExpand
            };
            laoyout.Children.Add(new Xamarin.Forms.Label
            {
                Text = "Fyll i de rätta svaren för att tända granen",
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    FontSize = 40,
                    WidthRequest = 1500,
                    HorizontalTextAlignment = TextAlignment.Center
            });
            var grid = new Grid
            {
                WidthRequest = 1500
            };
            Enumerable.Range(0, 6).ToList().ForEach(_ => grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }));
            Enumerable.Range(0, 4).ToList().ForEach(_ => grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }));
            for (int row = 0; row < 6; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    grid.Children.Add(CreateEntry(random, column, row, counter), column, row);
                    counter++;
                }

            }
            laoyout.Children.Add(grid);
            var button = new Xamarin.Forms.Button
            {
                Text = "Börja om"
            };
            button.Clicked += Reset;
            laoyout.Children.Add(button);
            var content = new ContentPage
            {
                Content = laoyout,
            };
            return content.GetOouiElement();
        }
        public void Publish()
        {
            UI.Publish("/addition", Create);
        }
        public void Check_Answer(object sender, EventArgs args)
        {
            var entry = sender as Entry;
            var text = entry.Text;
            var pin = _results[entry];
            if (entry != null && entry.Text.Length >= 2)
            {
                if (int.TryParse(entry.ClassId, out int correct) && int.TryParse(entry.Text, out int answer))
                {
                    if (correct == answer)
                    {
                        pin.correct = true;
                        entry.BackgroundColor = Xamarin.Forms.Color.Green;
                        _gpio.WritePin(pin.pin, pin.correct);
                    }
                    else
                    {
                        pin.correct = false;
                        entry.BackgroundColor = Xamarin.Forms.Color.Red;
                        _gpio.WritePin(pin.pin, pin.correct);
                    }
                    _results[entry] = pin;
                    if (_results.All(_ => _.Value.correct))
                    {
                        _blink = true;
                        Blink();
                    }
                }
            }
        }

        private void Reset(object sender, EventArgs args)
        {
            _blink = false;
            _gpio.WritePin(2, false);
            _results.Keys.ToList().ForEach(key =>
            {
                key.BackgroundColor = Xamarin.Forms.Color.White;
                var e = _results[key];
                e.correct = false;
                _gpio.WritePin(e.pin, false);
                _results[key] = e;
            });
        }

        private void Blink()
        {
            Task.Run(() =>
            {
                while (_blink)
                {
                    _gpio.WritePin(2, false);
                    Enumerable.Range(4, 23).ToList().ForEach(pin => _gpio.WritePin(pin, false));
                    Thread.Sleep(500);
                    _gpio.WritePin(2, true);
                    Enumerable.Range(4, 23).ToList().ForEach(pin => _gpio.WritePin(pin, true));
                }
            });

        }

        private StackLayout CreateEntry(Random random, int column, int row, int counter)
        {
            var l = new StackLayout
            {
                Orientation = StackOrientation.Horizontal
            };
            int left = random.Next(10, 100);
            int right = random.Next(1, 10);
            l.Children.Add(new Xamarin.Forms.Label { Text = left.ToString(), HorizontalTextAlignment = TextAlignment.Start });
            l.Children.Add(new Xamarin.Forms.Label { Text = "+", HorizontalTextAlignment = TextAlignment.Center });
            l.Children.Add(new Xamarin.Forms.Label { Text = right.ToString(), HorizontalTextAlignment = TextAlignment.End });
            var e = new Entry
            {
                Placeholder = "Skriv svaret här",
                WidthRequest = 150,
                ClassId = (left + right).ToString()
            };
            _results.Add(e, (counter, false));
            e.TextChanged += Check_Answer;
            l.Children.Add(e);
            return l;
        }
    }
}