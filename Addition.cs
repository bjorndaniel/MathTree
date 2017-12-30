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
        private bool _easy;
        private Random _random;
        private Dictionary<Entry, (int pin, bool correct)> _results = new Dictionary<Entry, (int pin, bool correct)>();
        private Gpio _gpio = new Gpio();
        private bool _blink;

        public Addition(bool easy = false)
        {
            _easy = easy;
            _random = new Random();
        }

        Ooui.Element Create()
        {
            var counter = 4;
            var layout = new StackLayout
            {
                Padding = new Thickness(50),
                HorizontalOptions = LayoutOptions.CenterAndExpand
            };
            layout.Children.Add(new Xamarin.Forms.Label
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
                    grid.Children.Add(CreateEntry(column, row, counter), column, row);
                    counter++;
                }
            }
            layout.Children.Add(grid);
            var button = new Xamarin.Forms.Button
            {
                Text = "Börja om"
            };
            button.Clicked += Reset;
            layout.Children.Add(button);
            var content = new ContentPage
            {
                Content = layout,
            };
            return content.GetOouiElement();
        }
        public void Publish()
        {
            UI.Publish("/addition", Create);
        }

        public void PublishEasy()
        {
            UI.Publish("/additioneasy", Create);
        }
        public void Check_Answer(object sender, EventArgs args)
        {
            var entry = sender as Entry;
            var text = entry.Text;
            var pin = _results[entry];
            if (entry != null && entry.Text.Length >= 1)
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
            _random = new Random();
            _blink = false;
            Thread.Sleep(1500);
            _gpio.WritePin(2, false);
            _results.Keys.ToList().ForEach(key =>
            {
                key.BackgroundColor = Xamarin.Forms.Color.White;
                var e = _results[key];
                e.correct = false;
                var question = CreateQuestion();
                key.ClassId = (question.left + question.right).ToString();
                var layout = key.Parent as StackLayout;
                var leftLabel = layout.Children.First(_ => _.ClassId == "Left") as Xamarin.Forms.Label;
                var rightLabel = layout.Children.First(_ => _.ClassId == "Right") as Xamarin.Forms.Label;
                leftLabel.Text = question.left.ToString();
                rightLabel.Text = question.right.ToString();
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
                    Enumerable.Range(4, 24).ToList().ForEach(pin => _gpio.WritePin(pin, false));
                    Thread.Sleep(500);
                    _gpio.WritePin(2, true);
                    Enumerable.Range(4, 24).ToList().ForEach(pin => _gpio.WritePin(pin, true));
                    Thread.Sleep(500);
                }
            });

        }

        private StackLayout CreateEntry(int column, int row, int counter)
        {
            var l = new StackLayout
            {
                Orientation = StackOrientation.Horizontal
            };
            var question = CreateQuestion();
            l.Children.Add(new Xamarin.Forms.Label { Text = question.left.ToString(), HorizontalTextAlignment = TextAlignment.Start, ClassId = "Left" });
            l.Children.Add(new Xamarin.Forms.Label { Text = "+", HorizontalTextAlignment = TextAlignment.Center });
            l.Children.Add(new Xamarin.Forms.Label { Text = question.right.ToString(), HorizontalTextAlignment = TextAlignment.End, ClassId = "Right" });
            var e = new Entry
            {
                Placeholder = "Skriv svaret här",
                WidthRequest = 150,
                ClassId = (question.left + question.right).ToString()
            };
            _results.Add(e, (counter, false));
            e.TextChanged += Check_Answer;
            l.Children.Add(e);
            return l;
        }

        private (int left, int right) CreateQuestion()
        {
            if (_easy)
            {
                return (_random.Next(1, 7), _random.Next(0, 3));
            }
            return (_random.Next(10, 100), _random.Next(1, 10));
        }
    }
}