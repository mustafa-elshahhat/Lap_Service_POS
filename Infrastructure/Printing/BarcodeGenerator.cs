using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AlJohary.ServiceHub.Infrastructure.Printing
{
    public static class BarcodeGenerator
    {
        private static readonly Dictionary<char, string> Patterns = new Dictionary<char, string>
        {
            {'0', "n,n,n,w,w,n,w,n,n"}, {'1', "w,n,n,w,n,n,n,n,w"}, {'2', "n,n,w,w,n,n,n,n,w"}, {'3', "w,n,w,w,n,n,n,n,n"},
            {'4', "n,n,n,w,n,n,w,n,w"}, {'5', "w,n,n,w,n,n,w,n,n"}, {'6', "n,n,w,w,n,n,w,n,n"}, {'7', "n,n,n,w,n,n,n,n,w"},
            {'8', "w,n,n,w,n,n,n,n,n"}, {'9', "n,n,w,w,n,n,n,n,n"}, {'A', "w,n,n,n,n,w,n,n,w"}, {'B', "n,n,w,n,n,w,n,n,w"},
            {'C', "w,n,w,n,n,w,n,n,n"}, {'D', "n,n,n,n,w,w,n,n,w"}, {'E', "w,n,n,n,w,w,n,n,n"}, {'F', "n,n,w,n,w,w,n,n,n"},
            {'G', "n,n,n,n,n,w,w,n,w"}, {'H', "w,n,n,n,n,w,w,n,n"}, {'I', "n,n,w,n,n,w,w,n,n"}, {'J', "n,n,n,n,w,w,w,n,n"},
            {'K', "w,n,n,n,n,n,n,w,w"}, {'L', "n,n,w,n,n,n,n,w,w"}, {'M', "w,n,w,n,n,n,n,w,n"}, {'N', "n,n,n,n,w,n,n,w,w"},
            {'O', "w,n,n,n,w,n,n,w,n"}, {'P', "n,n,w,n,w,n,n,w,n"}, {'Q', "n,n,n,n,n,n,w,w,w"}, {'R', "w,n,n,n,n,n,w,w,n"},
            {'S', "n,n,w,n,n,n,w,w,n"}, {'T', "n,n,n,n,w,n,w,w,n"}, {'U', "w,w,n,n,n,n,n,n,w"}, {'V', "n,w,w,n,n,n,n,n,w"},
            {'W', "w,w,w,n,n,n,n,n,n"}, {'X', "n,w,n,n,w,n,n,n,w"}, {'Y', "w,w,n,n,w,n,n,n,n"}, {'Z', "n,w,w,n,w,n,n,n,n"},
            {'-', "n,w,n,n,n,n,w,n,w"}, {'.', "w,w,n,n,n,n,w,n,n"}, {' ', "n,w,w,n,n,n,w,n,n"}, {'*', "n,w,n,n,w,n,w,n,n"},
            {'$', "n,w,n,w,n,w,n,n,n"}, {'/', "n,w,n,w,n,n,n,w,n"}, {'+', "n,w,n,n,n,w,n,w,n"}, {'%', "n,n,n,w,n,w,n,w,n"}
        };

        public static UIElement GenerateCode39(string content, double height = 50, double narrowBarWidth = 1.0)
        {
            if (string.IsNullOrEmpty(content)) return new Canvas();

            content = content.ToUpper();

            string fullContent = "*" + content + "*";

            Canvas canvas = new Canvas();
            canvas.Height = height;

            double currentX = 0;
            double wideFactor = 2.5;

            Rectangle bg = new Rectangle
            {
                Fill = Brushes.White,
                Height = height
            };

            
            foreach (char c in fullContent)
            {
                if (!Patterns.TryGetValue(c, out string patternStr))
                    continue;

                string[] elements = patternStr.Split(',');

                for (int i = 0; i < elements.Length; i++)
                {
                    bool isBar = (i % 2 == 0);
                    double width = (elements[i] == "w") ? (narrowBarWidth * wideFactor) : narrowBarWidth;

                    if (isBar)
                    {
                        Rectangle rect = new Rectangle
                        {
                            Width = width,
                            Height = height,
                            Fill = Brushes.Black,
                            SnapsToDevicePixels = true
                        };
                        Canvas.SetLeft(rect, currentX);
                        canvas.Children.Add(rect);
                    }

                    currentX += width;
                }

                currentX += narrowBarWidth;
            }

            canvas.Width = currentX;
            bg.Width = currentX;
            canvas.Children.Insert(0, bg);

            return canvas;
        }
    }
}
