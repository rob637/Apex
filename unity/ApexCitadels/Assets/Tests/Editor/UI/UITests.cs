using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ApexCitadels.Tests.Editor.UI
{
    /// <summary>
    /// Unit tests for UI system components.
    /// Tests tooltips, animations, panels, and UI utilities.
    /// </summary>
    [TestFixture]
    public class UITests : TestBase
    {
        #region Tooltip Tests

        [Test]
        public void Tooltip_Format_HandlesSimpleText()
        {
            // Arrange
            string text = "Simple tooltip text";
            
            // Act
            string formatted = FormatTooltip(text);
            
            // Assert
            Assert.AreEqual("Simple tooltip text", formatted);
        }

        [Test]
        public void Tooltip_Format_HandlesColorTags()
        {
            // Arrange
            string text = "{gold}100 Gold{/gold}";
            
            // Act
            string formatted = FormatTooltip(text);
            
            // Assert
            Assert.IsTrue(formatted.Contains("<color=#FFD700>"));
            Assert.IsTrue(formatted.Contains("</color>"));
        }

        [Test]
        public void Tooltip_Format_HandlesMultipleTags()
        {
            // Arrange
            string text = "{damage}50{/damage} {healing}+20{/healing}";
            
            // Act
            string formatted = FormatTooltip(text);
            
            // Assert
            Assert.IsTrue(formatted.Contains("50"));
            Assert.IsTrue(formatted.Contains("+20"));
        }

        #endregion

        #region Number Formatting Tests

        [Test]
        public void FormatNumber_SmallNumber_NoAbbreviation()
        {
            // Arrange/Act
            string formatted = FormatLargeNumber(999);
            
            // Assert
            Assert.AreEqual("999", formatted);
        }

        [Test]
        public void FormatNumber_Thousands_UsesK()
        {
            // Arrange/Act
            string formatted = FormatLargeNumber(1500);
            
            // Assert
            Assert.AreEqual("1.5K", formatted);
        }

        [Test]
        public void FormatNumber_Millions_UsesM()
        {
            // Arrange/Act
            string formatted = FormatLargeNumber(1500000);
            
            // Assert
            Assert.AreEqual("1.5M", formatted);
        }

        [Test]
        public void FormatNumber_Billions_UsesB()
        {
            // Arrange/Act
            string formatted = FormatLargeNumber(2500000000);
            
            // Assert
            Assert.AreEqual("2.5B", formatted);
        }

        #endregion

        #region Time Formatting Tests

        [Test]
        public void FormatTime_Seconds_ShowsSecondsOnly()
        {
            // Arrange/Act
            string formatted = FormatTimeSpan(30);
            
            // Assert
            Assert.AreEqual("30s", formatted);
        }

        [Test]
        public void FormatTime_Minutes_ShowsMinutesAndSeconds()
        {
            // Arrange/Act
            string formatted = FormatTimeSpan(150);
            
            // Assert
            Assert.AreEqual("2m 30s", formatted);
        }

        [Test]
        public void FormatTime_Hours_ShowsHoursMinutesSeconds()
        {
            // Arrange/Act
            string formatted = FormatTimeSpan(3665);
            
            // Assert
            Assert.AreEqual("1h 1m 5s", formatted);
        }

        [Test]
        public void FormatTime_Days_ShowsDaysAndHours()
        {
            // Arrange/Act
            string formatted = FormatTimeSpan(90000);
            
            // Assert
            Assert.IsTrue(formatted.Contains("d"));
            Assert.IsTrue(formatted.Contains("h"));
        }

        #endregion

        #region Animation Easing Tests

        [Test]
        public void EaseLinear_Midpoint_Returns50Percent()
        {
            // Arrange/Act
            float result = EaseLinear(0.5f);
            
            // Assert
            Assert.AreEqual(0.5f, result, 0.01f);
        }

        [Test]
        public void EaseInQuad_Midpoint_LessThan50Percent()
        {
            // Arrange/Act
            float result = EaseInQuad(0.5f);
            
            // Assert
            Assert.Less(result, 0.5f, "EaseIn should be slower at start");
        }

        [Test]
        public void EaseOutQuad_Midpoint_GreaterThan50Percent()
        {
            // Arrange/Act
            float result = EaseOutQuad(0.5f);
            
            // Assert
            Assert.Greater(result, 0.5f, "EaseOut should be faster at start");
        }

        [Test]
        public void EaseInOutQuad_Endpoints_CorrectValues()
        {
            // Assert
            Assert.AreEqual(0f, EaseInOutQuad(0f), 0.01f);
            Assert.AreEqual(1f, EaseInOutQuad(1f), 0.01f);
        }

        #endregion

        #region Color Utility Tests

        [Test]
        public void HexToColor_ValidHex_ReturnsCorrectColor()
        {
            // Arrange/Act
            Color color = HexToColor("#FF0000");
            
            // Assert
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0f, color.g, 0.01f);
            Assert.AreEqual(0f, color.b, 0.01f);
        }

        [Test]
        public void HexToColor_WithoutHash_StillWorks()
        {
            // Arrange/Act
            Color color = HexToColor("00FF00");
            
            // Assert
            Assert.AreEqual(0f, color.r, 0.01f);
            Assert.AreEqual(1f, color.g, 0.01f);
            Assert.AreEqual(0f, color.b, 0.01f);
        }

        [Test]
        public void ColorToHex_ReturnsValidHex()
        {
            // Arrange/Act
            string hex = ColorToHex(Color.blue);
            
            // Assert
            Assert.AreEqual("#0000FF", hex.ToUpper());
        }

        [Test]
        public void LerpColor_Midpoint_ReturnsBlendedColor()
        {
            // Arrange
            Color a = Color.black;
            Color b = Color.white;
            
            // Act
            Color result = Color.Lerp(a, b, 0.5f);
            
            // Assert
            Assert.AreEqual(0.5f, result.r, 0.01f);
            Assert.AreEqual(0.5f, result.g, 0.01f);
            Assert.AreEqual(0.5f, result.b, 0.01f);
        }

        #endregion

        #region Layout Tests

        [Test]
        public void GridLayout_CalculatesCorrectCellCount()
        {
            // Arrange
            int totalItems = 25;
            int columns = 4;
            
            // Act
            int rows = Mathf.CeilToInt((float)totalItems / columns);
            
            // Assert
            Assert.AreEqual(7, rows);
        }

        [Test]
        public void GridLayout_CellPosition_CalculatesCorrectly()
        {
            // Arrange
            int index = 7;
            int columns = 4;
            float cellWidth = 100;
            float cellHeight = 80;
            
            // Act
            int row = index / columns;
            int col = index % columns;
            Vector2 position = new Vector2(col * cellWidth, row * cellHeight);
            
            // Assert
            Assert.AreEqual(300, position.x); // Column 3
            Assert.AreEqual(80, position.y);  // Row 1
        }

        #endregion

        #region Panel State Tests

        [Test]
        public void PanelStack_Push_AddsToStack()
        {
            // Arrange
            var stack = new TestPanelStack();
            
            // Act
            stack.Push("MainMenu");
            stack.Push("Settings");
            
            // Assert
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual("Settings", stack.Current);
        }

        [Test]
        public void PanelStack_Pop_RemovesFromStack()
        {
            // Arrange
            var stack = new TestPanelStack();
            stack.Push("MainMenu");
            stack.Push("Settings");
            
            // Act
            string popped = stack.Pop();
            
            // Assert
            Assert.AreEqual("Settings", popped);
            Assert.AreEqual("MainMenu", stack.Current);
        }

        [Test]
        public void PanelStack_PopEmpty_ReturnsNull()
        {
            // Arrange
            var stack = new TestPanelStack();
            
            // Act
            string popped = stack.Pop();
            
            // Assert
            Assert.IsNull(popped);
        }

        #endregion

        #region Helper Methods

        private string FormatTooltip(string text)
        {
            // Replace custom tags with Unity rich text
            text = text.Replace("{gold}", "<color=#FFD700>");
            text = text.Replace("{/gold}", "</color>");
            text = text.Replace("{damage}", "<color=#FF4444>");
            text = text.Replace("{/damage}", "</color>");
            text = text.Replace("{healing}", "<color=#44FF44>");
            text = text.Replace("{/healing}", "</color>");
            return text;
        }

        private string FormatLargeNumber(long number)
        {
            if (number >= 1000000000)
                return $"{number / 1000000000f:0.#}B";
            if (number >= 1000000)
                return $"{number / 1000000f:0.#}M";
            if (number >= 1000)
                return $"{number / 1000f:0.#}K";
            return number.ToString();
        }

        private string FormatTimeSpan(int totalSeconds)
        {
            if (totalSeconds >= 86400)
            {
                int days = totalSeconds / 86400;
                int hours = (totalSeconds % 86400) / 3600;
                return $"{days}d {hours}h";
            }
            if (totalSeconds >= 3600)
            {
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                int seconds = totalSeconds % 60;
                return $"{hours}h {minutes}m {seconds}s";
            }
            if (totalSeconds >= 60)
            {
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                return $"{minutes}m {seconds}s";
            }
            return $"{totalSeconds}s";
        }

        private float EaseLinear(float t) => t;
        private float EaseInQuad(float t) => t * t;
        private float EaseOutQuad(float t) => t * (2 - t);
        private float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

        private Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        private string ColorToHex(Color color)
        {
            return $"#{(int)(color.r * 255):X2}{(int)(color.g * 255):X2}{(int)(color.b * 255):X2}";
        }

        #endregion

        #region Test Data Classes

        private class TestPanelStack
        {
            private Stack<string> _stack = new Stack<string>();

            public int Count => _stack.Count;
            public string Current => _stack.Count > 0 ? _stack.Peek() : null;

            public void Push(string panelName)
            {
                _stack.Push(panelName);
            }

            public string Pop()
            {
                return _stack.Count > 0 ? _stack.Pop() : null;
            }
        }

        #endregion
    }
}
