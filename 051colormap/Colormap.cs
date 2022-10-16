using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MathSupport;

namespace _051colormap
{
  /// <summary>
  /// Helper class used to count frequencies of detected colors, contains RGB description and optionally the color's hue.
  /// </summary>
  class CustomColor
  {
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public int Count { get; set; }
    public double H { get; set; }

    public CustomColor(int r, int g, int b, int count)
    {
      R = r;
      G = g;
      B = b;
      this.Count = count;
    }
  }

  class Colormap
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    public static void InitForm (out string author)
    {
      author = "Lukáš Polák";
    }

    /// <summary>
    /// Generate a colormap based on input image.
    /// </summary>
    /// <param name="input">Input raster image.</param>
    /// <param name="numCol">Required colormap size (ignore it if you must).</param>
    /// <param name="colors">Output palette (array of colors).</param>
    public static void Generate (Bitmap input, int numCol, out Color[] colors)
    {
      int tolerance = 70; //we start off with a high tolerance because of colorful images

      List<CustomColor> colorlist = ComputeCustomColorPalette(input.Width, input.Height, tolerance, input);
      while(colorlist.Count < numCol) //it may happen that the tolerance was too high, so we try to compute more colors
      {
        colorlist = ComputeCustomColorPalette(input.Width, input.Height, tolerance, input);
        tolerance -= 10;
        if (tolerance < 10) break; //prevent negatives
      }

      List<CustomColor> sortedColors = colorlist.OrderByDescending(o => o.Count).ToList(); //sort colors based on their frequency

      colors = new Color[numCol];

      //it may happen that there is still a small number of colors for b&w images
      //then try to use the linear interpolation to generate at least some colors from the first (two)...
      if(sortedColors.Count < numCol)
      {
        if (sortedColors.Count == 1)
        {
          colors = ComputeLinearInterpolation(Color.FromArgb(sortedColors[0].R, sortedColors[0].G, sortedColors[0].B), Color.FromArgb(0, 0, 0), numCol);
        }
        else
        {
          colors = ComputeLinearInterpolation(Color.FromArgb(sortedColors[0].R, sortedColors[0].G, sortedColors[0].B), Color.FromArgb(sortedColors[1].R, sortedColors[1].G, sortedColors[1].B), numCol);
        }
        return;
      }

      //calculate hue for the final picked colors
      for (int i = 0; i < numCol; i++)
      {
        Arith.ColorToHSV(Color.FromArgb(sortedColors[i].R, sortedColors[i].G, sortedColors[i].B), out double H, out double S, out double V);
        sortedColors[i].H = H;
      }

      //sort colors by hue
      sortedColors = sortedColors.OrderByDescending(o => o.H).ToList();

      //compute the final array
      for(int i = 0; i < numCol; i++)
      {
        colors[i] = Color.FromArgb(sortedColors[i].R, sortedColors[i].G, sortedColors[i].B);
      }
    }

    /// <summary>
    /// Linear interpolation. Should be used ONLY when there is a very small number of colors present in the image.
    /// </summary>
    /// <param name="first">First color.</param>
    /// <param name="second">Second color.</param>
    /// <param name="numCol">Number of colors to be generated.</param>
    /// <returns>An array with generated colors.</returns>
    public static Color[] ComputeLinearInterpolation(Color first, Color second, int numCol)
    {
      Color[] colors = new Color[numCol];

      colors[0] = first;

      double H, S, V;
      Arith.ColorToHSV(second, out H, out S, out V);
      if (S > 1.0e-3)
        colors[numCol - 1] = Arith.HSVToColor(H, 1.0, 1.0);   // non-monochromatic color => using Hue only
      else
        colors[numCol - 1] = second;                          // monochromatic color => using it directly

      // color-ramp linear interpolation:
      float r = colors[0].R;
      float g = colors[0].G;
      float b = colors[0].B;
      float dr = (colors[numCol - 1].R - r) / (numCol - 1.0f);
      float dg = (colors[numCol - 1].G - g) / (numCol - 1.0f);
      float db = (colors[numCol - 1].B - b) / (numCol - 1.0f);

      for (int i = 1; i < numCol; i++)
      {
        r += dr;
        g += dg;
        b += db;
        colors[i] = Color.FromArgb((int)r, (int)g, (int)b);
      }

      return colors;
    }

    /// <summary>
    /// Method used to compute custom color palette from the given input. It scans through the image pixels (every fifth pixel, may be changed).
    /// Then it detects similar colors using RGB comparsion with a given tolerance. Then it returns a list with color groups found in the image.
    /// </summary>
    /// <param name="width">Bitmap width.</param>
    /// <param name="height">Bitmap height.</param>
    /// <param name="tolerance">Similarity of the colors.</param>
    /// <param name="input">Bitmap input image.</param>
    /// <returns>A list containing color groups.</returns>
    public static List<CustomColor> ComputeCustomColorPalette(int width, int height, int tolerance, Bitmap input)
    {
      List<CustomColor> colorlist = new List<CustomColor>();

      int delta = 5;

      for (int i = 0; i < width; i += delta)
      {
        for (int y = 0; y < height; y += delta)
        {
          Color c = input.GetPixel(i, y);
          //look for a similar color based on the tolerance
          CustomColor colorFromList = colorlist.Find(item => Math.Abs(item.R - c.R) < tolerance && Math.Abs(item.B - c.B) < tolerance && Math.Abs(item.G - c.G) < tolerance);
          if (colorFromList != null)
          {
            colorFromList.Count++;
          }
          else
          {
            colorlist.Add(new CustomColor(c.R, c.G, c.B, 1));
          }
        }
      }

      return colorlist;
    }
  }
}
