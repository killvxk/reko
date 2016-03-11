﻿#region License
/* 
 * Copyright (C) 1999-2016 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Reko.Gui.Windows.Controls
{
    public class TextViewLayout
    {
        SortedList<float, LayoutLine> visibleLines;

        private TextViewLayout(SortedList<float, LayoutLine> visibleLines)
        {
            this.visibleLines = visibleLines;
        }

        /// <summary>
        /// Generates a TextViewLayout from all the lines in the model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="g"></param>
        /// <param name="defaultFont"></param>
        /// <param name="styleStack"></param>
        /// <returns></returns>
        public static TextViewLayout AllLines(TextViewModel model, Graphics g, Font defaultFont, StyleStack styleStack)
        {
            model.MoveToLine(model.StartPosition, 0);
            var rcLine = new RectangleF();
            var builder = new Builder(model, g, styleStack, defaultFont);
            for (;;)
            {
                var lines = model.GetLineSpans(1);
                if (lines.Length == 0)
                    break;
                builder.AddLayoutLine(lines[0], ref rcLine);
            }
            return builder.Build();
        }

        public static TextViewLayout VisibleLines(TextViewModel model, Size size, Graphics g, Font defaultFont, StyleStack styleStack)
        {
            var szClient = new SizeF(size);
            var rcLine = new RectangleF(0, 0, szClient.Width, 0);

            var builder = new Builder(model, g, styleStack, defaultFont);
            while (rcLine.Top < szClient.Height)
            {
                var lines = model.GetLineSpans(1);
                if (lines.Length == 0)
                    break;
                builder.AddLayoutLine(lines[0], ref rcLine);
            }
            return builder.Build();
        }

        private class Builder
        {
            private Graphics g;
            private TextViewModel model;
            private StyleStack styleStack;
            private Font defaultFont;
            private SortedList<float, LayoutLine> visibleLines;

            public Builder(TextViewModel model, Graphics g, StyleStack styleStack, Font defaultFont)
            {
                this.model = model;
                this.g = g;
                this.styleStack = styleStack;
                this.defaultFont = defaultFont;
                this.visibleLines = new SortedList<float, LayoutLine>();
            }

            public void AddLayoutLine(LineSpan line, ref RectangleF rcLine /* put in state */)
            {
                float cyLine = MeasureLineHeight(line);
                rcLine.Height = cyLine;
                var ll = new LayoutLine(line.Position)
                {
                    Extent = rcLine,
                    Spans = ComputeSpanLayouts(line.TextSpans, rcLine)
                };
                this.visibleLines.Add(rcLine.Top, ll);
                rcLine.Offset(0, cyLine);
            }

            private float MeasureLineHeight(LineSpan line)
            {
                float height = 0.0F;
                foreach (var span in line.TextSpans)
                {
                    styleStack.PushStyle(span.Style);
                    var font = styleStack.GetFont(defaultFont);
                    height = Math.Max(height, font.Height);
                    styleStack.PopStyle();
                }
                return height;
            }

            /// <summary>
            /// Computes the layout for a line of spans.
            /// </summary>
            /// <param name="spans"></param>
            /// <param name="rcLine"></param>
            /// <param name="g"></param>
            /// <returns></returns>
            private LayoutSpan[] ComputeSpanLayouts(IEnumerable<TextSpan> spans, RectangleF rcLine)
            {
                var spanLayouts = new List<LayoutSpan>();
                var pt = new PointF(rcLine.Left, rcLine.Top);
                foreach (var span in spans)
                {
                    styleStack.PushStyle(span.Style);
                    var text = span.GetText();
                    var font = styleStack.GetFont(defaultFont);
                    var szText = GetSize(span, text, font, g);
                    var rc = new RectangleF(pt, szText);
                    spanLayouts.Add(new LayoutSpan
                    {
                        Extent = rc,
                        Style = span.Style,
                        Text = text,
                        ContextMenuID = span.ContextMenuID,
                        Tag = span.Tag,
                    });
                    pt.X = pt.X + szText.Width;
                    styleStack.PopStyle();
                }
                return spanLayouts.ToArray();
            }

            /// <summary>
            /// Computes the size of a text span.
            /// </summary>
            /// <remarks>
            /// The span is first asked to measure itself, then the current style is 
            /// allowed to override the size.
            /// </remarks>
            /// <param name="span"></param>
            /// <param name="text"></param>
            /// <param name="font"></param>
            /// <param name="g"></param>
            /// <returns></returns>
            private SizeF GetSize(TextSpan span, string text, Font font, Graphics g)
            {
                var size = span.GetSize(text, font, g);
                int? width = styleStack.GetWidth();
                if (width.HasValue)
                {
                    size.Width = width.Value;
                }
                return size;
            }

            public TextViewLayout Build()
            {
                return new TextViewLayout(visibleLines);
            }
        }

    }

    /// <summary>
    /// Line of spans.
    /// </summary>
    public class LayoutLine
    {
        public LayoutLine(object Position) { this.Position = Position; }
        public object Position;
        public RectangleF Extent;
        public LayoutSpan[] Spans;
    }

    /// <summary>
    /// Horizontal span of text
    /// </summary>
    public class LayoutSpan
    {
        public RectangleF Extent;
        public string Text;
        public string Style;
        public object Tag;
        public int ContextMenuID;
    }

}